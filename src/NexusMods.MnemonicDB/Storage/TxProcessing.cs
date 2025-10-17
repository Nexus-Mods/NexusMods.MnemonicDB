using System;
using System.Collections.Generic;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.Storage;

public partial class DatomStore
{
    private record struct ManyKey(EntityId E, AttributeId A, object V);

    private bool TryGetCurrentSingleWithTxId(EntityId e, AttributeId a, out TaggedValue value, out TxId t)
    {
        var slice = SliceDescriptor.Create(e, a);
        using var iterator = _currentDb!.LightweightDatoms(slice);
        if (iterator.MoveNext())
        {
            var datom = ValueDatom.Create(iterator);
            value = datom.TaggedValue;
            t = iterator.T;
            return true;
        }
        value = default;
        t = default;
        return false;
    }

    private bool TryOwnerOfUnique(AttributeId a, TaggedValue v, out EntityId e)
    {
        using var writer = new PooledMemoryBufferWriter();
        v.Tag.Write(v.Value, writer);
        using var iterator = _currentDb!.LightweightDatoms(SliceDescriptor.Create(a, v.Tag, writer.WrittenMemory));
        if (!iterator.MoveNext())
        {
            e = default;
            return false;
        }

        e = iterator.Prefix.E;
        return true;
    }

    /// <summary>
    /// Normalizes the datoms in the input list. Normalization involves the following:
    /// * Retracts are added for any cardinality one updates
    /// * Datoms attached to EntityIds in the temp partition are assumed to not need implicit retracts
    /// * Datoms for cardinaltiy many attributes do not have implied retracts
    /// * Datoms are applied the order they are given to this function
    /// * Set/Retract/Set for the same EntityId/Attribute are reduced down to a single Set
    /// * Set/Set/Set is normaled down to the final Set
    /// * Unique constraints that are violated throw an exception, there is no implied retract for
    ///   unique attributes
    /// * Any retracted datoms (including the TxId) are logged in ToRetract
    /// </summary>
    /// <param name="datoms"></param>
    /// <returns></returns>
    private (DatomList Retracts, DatomList Asserts) NormalizeWithTxIds(ReadOnlySpan<ValueDatom> datoms)
    {
        // ---- PASS 1: collect final intent ----

        // Card-1: last assert per (E,A)
        var lastAssert1 = new Dictionary<(EntityId E, AttributeId A), TaggedValue>();
        var seenEA1 = new HashSet<(EntityId E, AttributeId A)>();

        // Card-many: last op per (E,A,V): true = assert, false = retract
        var lastOpMany = new Dictionary<(EntityId E, AttributeId A, TaggedValue V), bool>();
        var seenEAx = new HashSet<(EntityId E, AttributeId A)>();

        foreach (var d in datoms)
        {
            var keyEA = (d.Prefix.E, d.Prefix.A);

            if (!_attributeCache.IsCardinalityMany(d.Prefix.A))
            {
                seenEA1.Add(keyEA);
                if (!d.Prefix.IsRetract) lastAssert1[keyEA] = d.TaggedValue; // last assertion wins
            }
            else
            {
                seenEAx.Add(keyEA);
                lastOpMany[(d.Prefix.E, d.Prefix.A, d.TaggedValue)] = !d.Prefix.IsRetract; // last op for this value
            }
        }

        // ---- PASS 2: emit minimal deltas (capturing old txids for retracts) ----

        var retracts = new DatomList(_attributeCache);
        var asserts  = new DatomList(_attributeCache);

        // Dedup retracts by (E,A,V) (txid is implied by snapshot's current)
        var haveRetract = new HashSet<(EntityId E, AttributeId A, object V)>();

        // Card-1
        foreach (var (E, A) in seenEA1)
        {
            var haveFinal = lastAssert1.TryGetValue((E, A), out var final);

            if (E.InPartition(PartitionId.Temp))
            {
                // Temp: no old lookup, never retract
                if (haveFinal) 
                    asserts.Add(ValueDatom.Create(E, A, final.Tag, final.Value));
                continue;
            }

            var haveOld = TryGetCurrentSingleWithTxId(E, A, out var old, out var oldTx);

            if (!Equals(final, old))
            {
                if (haveOld)
                {
                    if (haveRetract.Add((E, A, old)))
                        retracts.Add(ValueDatom.Create(E, A, old, oldTx));
                }

                if (haveFinal)
                    asserts.Add(ValueDatom.Create(E, A, final));
            }
        }

        // Card-many
        foreach (var (E, A) in seenEAx)
        {
            // Build FINAL set: values whose last op was "assert"
            var finalSet = new HashSet<TaggedValue>();
            foreach (var kv in lastOpMany)
            {
                if (Equals(kv.Key.E, E) && Equals(kv.Key.A, A) && kv.Value)
                    finalSet.Add(kv.Key.V);
            }

            if (E.InPartition(PartitionId.Temp))
            {
                // Temp: no old values; only add new
                foreach (var v in finalSet)
                    asserts.Add(ValueDatom.Create(E, A, v));
                continue;
            }

            // OLD: dictionary value -> txid for direct emit on retract
            var oldMap = new Dictionary<TaggedValue, TxId>();
            {
                using var iterator = _currentDb!.LightweightDatoms(SliceDescriptor.Create(E, A));
                while (iterator.MoveNext())
                {
                    var valueTag = iterator.Prefix.ValueTag;
                    var value = new TaggedValue(valueTag, valueTag.Read<object>(iterator.ValueSpan));
                    if (!oldMap.ContainsKey(value)) oldMap[value] = iterator.Prefix.T; // defensive: keep first                
                }
            }

            // Preserve existing values unless explicitly retracted in this transaction
            foreach (var (v, _) in oldMap)
            {
                if (!lastOpMany.TryGetValue((E, A, v), out var isAssert))
                {
                    // no op on this value -> keep it
                    finalSet.Add(v);
                }
                else if (isAssert)
                {
                    // explicitly asserted -> keep it
                    finalSet.Add(v);
                }
                // else: explicitly retracted -> do not add; will be retracted below
            }


            // final \ old -> asserts
            foreach (var v in finalSet)
                if (!oldMap.ContainsKey(v))
                    asserts.Add(ValueDatom.Create(E, A, v));

            // old \ final -> retracts (with old txid)
            foreach (var (v, txid) in oldMap)
            {
                if (!finalSet.Contains(v) && haveRetract.Add((E, A, v)))
                    retracts.Add(ValueDatom.Create(E, A, v, txid));
            }
        }

        // ---- PASS 3: strict uniqueness (throw on conflict) ----

        var txClaim = new Dictionary<(AttributeId A, object V), EntityId>();

        foreach (var a in asserts)
        {
            if (!_attributeCache.IsUnique(a.Prefix.A)) continue;
            var key = (a.Prefix.A, a.Value);

            if (txClaim.TryGetValue(key, out var prevE) && !Equals(prevE, a.Prefix.E))
                throw new Exception(
                    $"Unique constraint violation in transaction: attribute {a.Prefix.A}, value '{a.Value}' asserted by entities {prevE} and {a.Prefix.E}.");

            txClaim[key] = a.Prefix.E;

            if (TryOwnerOfUnique(a.Prefix.A, a.TaggedValue, out var owner) && !Equals(owner, a.Prefix.E))
                throw new Exception(
                    $"Unique constraint violation: attribute {a.Prefix.A}, value '{a.Value}' already owned by entity {owner}, cannot assert for entity {a.Prefix.E}.");
        }

        // Retracts-first then asserts is still a safe apply order for your index moves.
        return (retracts, asserts);
    }

}
