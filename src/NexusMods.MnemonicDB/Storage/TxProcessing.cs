using System;
using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using System.Runtime.InteropServices;
using static NexusMods.MnemonicDB.Abstractions.IndexType;

namespace NexusMods.MnemonicDB.Storage;

public static class TxProcessing
{
    private record struct ManyKey(EntityId E, AttributeId A, object V);

    private static bool TryGetCurrentSingleWithTxId(IDatomsIndex index, EntityId e, AttributeId a, out TaggedValue value, out TxId t)
    {
        var slice = SliceDescriptor.Create(e, a);
        using var iterator = index.LightweightDatoms(slice);
        if (iterator.MoveNext())
        {
            var datom = Datom.Create(iterator);
            value = datom.TaggedValue;
            t = iterator.T;
            return true;
        }
        value = default;
        t = default;
        return false;
    }

    private static bool TryOwnerOfUnique(IDatomsIndex index, AttributeId a, TaggedValue v, out EntityId e)
    {
        using var slice = SliceDescriptor.Create(a, v.Tag, v.Value);
        using var iterator = index.LightweightDatoms(slice);
        if (!iterator.MoveNext())
        {
            e = default;
            return false;
        }

        e = iterator.Prefix.E;
        return true;
    }

    private static int FindTxFn(ReadOnlySpan<Datom> datoms)
    {
        for (int i = 0; i < datoms.Length; i++)
        {
            if (datoms[i].Tag == ValueTag.TxFunction)
                return i;
        }
        return -1;
    }

    public static (Datoms Retracts, Datoms Asserts) RunTxFnsAndNormalize(ReadOnlySpan<Datom> datoms, IDb basisDb, TxId thisTxId)
    {
        // Collect applied datoms (excluding tx-function markers)
        var applied = new Datoms(basisDb);

        // Pending tx functions generated during execution that must run before continuing
        var pending = new Queue<Datom>();

        void ExecuteTxFn(Datom fnDatom)
        {
            // Capture count to find newly-added datoms
            var start = applied.Count;

            // Support both delegate-style and ITxFunction-style
            switch (fnDatom.Value)
            {
                case Action<Datoms, IDb> action:
                    action(applied, basisDb);
                    break;
                case ITxFunction itx:
                {
                    using var tx = new InternalTransaction(basisDb, applied);
                    itx.Apply(tx, basisDb);
                    // Allow any temporary entities to materialize their datoms
                    tx.ProcessTemporaryEntities();
                    break;
                }
                default:
                    throw new InvalidOperationException("Unsupported TxFunction payload type: " + fnDatom.Value?.GetType().FullName);
            }

            // Scan newly-added datoms for nested tx functions; queue them in insertion order
            // then remove them from 'applied' without disturbing order of non-functions
            if (applied.Count > start)
            {
                var newFnDatoms = new List<Datom>();
                var removeIdx = new List<int>();
                for (int i = start; i < applied.Count; i++)
                {
                    var d = applied[i];
                    if (d.Tag == ValueTag.TxFunction)
                    {
                        newFnDatoms.Add(d);
                        removeIdx.Add(i);
                    }
                }
                // Remove from the end to preserve indices
                for (int i = removeIdx.Count - 1; i >= 0; i--)
                    applied.RemoveAt(removeIdx[i]);
                // Enqueue in the order they were added
                foreach (var d in newFnDatoms)
                    pending.Enqueue(d);
            }
        }

        // Process original datoms in order, interleaving any generated tx functions immediately
        for (int i = 0; i < datoms.Length; i++)
        {
            var d = datoms[i];
            if (d.Tag == ValueTag.TxFunction)
            {
                ExecuteTxFn(d);
            }
            else
            {
                applied.Add(d);
            }

            // Drain any queued tx functions before moving on, preserving order
            while (pending.Count > 0)
            {
                var nextFn = pending.Dequeue();
                ExecuteTxFn(nextFn);
            }
        }

        var span = CollectionsMarshal.AsSpan(applied);
        return NormalizeWithTxIds(span, basisDb, thisTxId);
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
    public static (Datoms Retracts, Datoms Asserts) NormalizeWithTxIds(ReadOnlySpan<Datom> datoms, IDatomsIndex index, TxId thisTxId)
    {
        var attributeResolver = index.AttributeResolver;
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

            if (!attributeResolver.AttributeCache.IsCardinalityMany(d.Prefix.A))
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

        var retracts = new Datoms(attributeResolver);
        var asserts  = new Datoms(attributeResolver);

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
                    asserts.Add(Datom.Create(E, A, final.Tag, final.Value));
                continue;
            }

            var haveOld = TryGetCurrentSingleWithTxId(index, E, A, out var old, out var oldTx);

            if (!Equals(final, old))
            {
                if (haveOld)
                {
                    if (haveRetract.Add((E, A, old)))
                        retracts.Add(Datom.Create(E, A, old, oldTx));
                }

                if (haveFinal)
                    asserts.Add(Datom.Create(E, A, final, thisTxId));
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
                    asserts.Add(Datom.Create(E, A, v, thisTxId));
                continue;
            }

            // OLD: dictionary value -> txid for direct emit on retract
            var oldMap = new Dictionary<TaggedValue, TxId>();
            {
                using var iterator = index.LightweightDatoms(SliceDescriptor.Create(E, A));
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
                    asserts.Add(Datom.Create(E, A, v, thisTxId));

            // old \ final -> retracts (with old txid)
            foreach (var (v, txid) in oldMap)
            {
                if (!finalSet.Contains(v) && haveRetract.Add((E, A, v)))
                    retracts.Add(Datom.Create(E, A, v, txid));
            }
        }

        // ---- PASS 3: strict uniqueness (throw on conflict) ----

        var txClaim = new Dictionary<(AttributeId A, object V), EntityId>();

        // Build a quick lookup for unique attribute values that this transaction retracts
        var uniqueRetractsThisTx = new HashSet<(AttributeId A, object V, EntityId E)>();
        foreach (var r in retracts)
        {
            if (!attributeResolver.AttributeCache.IsUnique(r.Prefix.A)) continue;
            uniqueRetractsThisTx.Add((r.Prefix.A, r.Value, r.Prefix.E));
        }

        foreach (var a in asserts)
        {
            if (!attributeResolver.AttributeCache.IsUnique(a.Prefix.A)) continue;
            var key = (a.Prefix.A, a.Value);

            if (txClaim.TryGetValue(key, out var prevE) && !Equals(prevE, a.Prefix.E))
                throw new UniqueConstraintException(a, prevE);
            
            txClaim[key] = a.Prefix.E;

            if (TryOwnerOfUnique(index, a.Prefix.A, a.TaggedValue, out var owner) && !Equals(owner, a.Prefix.E))
            {
                // Allow moving ownership within the same transaction: if this tx also retracts
                // the unique value from the current owner, do not throw.
                if (!uniqueRetractsThisTx.Contains((a.Prefix.A, a.Value, owner)))
                    throw new UniqueConstraintException(a, owner);
            }
        }

        // Retracts-first then asserts is still a safe apply order for your index moves.
        return (retracts, asserts);
    }

    /// <summary>
    /// Logs all the puts for asserting a datom into the current index of the database
    /// </summary>
    public static void LogAssert(IWriteBatch batch, in Datom assert, AttributeResolver resolver)
    {
        batch.Add(assert.With(TxLog));
        batch.Add(assert.With(EAVTCurrent));
        batch.Add(assert.With(AEVTCurrent));
        if (resolver.AttributeCache.IsIndexed(assert.Prefix.A))
            batch.Add(assert.With(AVETCurrent));
        if (assert.Prefix.ValueTag == ValueTag.Reference)
            batch.Add(assert.With(VAETCurrent));
    }
    
    /// <summary>
    /// Logs all the puts/deletes for updating the state of a retracted datom
    /// </summary>
    public static void LogRetract(IWriteBatch batch, in Datom oldDatom, TxId newTxId, AttributeResolver resolver)
    {
        Debug.Assert(!oldDatom.Prefix.IsRetract, "The datom handed to LogRetract is expected to be the old datom");
        Debug.Assert(oldDatom.Prefix.T < newTxId, "The datom handed to LogRetract is expected to be the old datom");
        
        var a = oldDatom.Prefix.A;
        var cache = resolver.AttributeCache;
        var isIndexed = cache.IsIndexed(a);
        var isReference = cache.IsReference(a);
        
        // First we issue deletes for all the old datoms
        batch.Delete(oldDatom.With(EAVTCurrent));
        batch.Delete(oldDatom.With(AEVTCurrent));
        if (isIndexed) 
            batch.Delete(oldDatom.With(AVETCurrent));
        if (isReference)
            batch.Delete(oldDatom.With(VAETCurrent));

        // The retract datom is the input datom, marked as an retract and with the TxId of the retracting transaction
        var retractDatom = oldDatom.With(newTxId).WithRetract(true);
        // Add the retract datom to the tx log
        batch.Add(retractDatom.With(TxLog));
        
        // Now we move the datom into the history index, but only if the attribute is not a no history attribute
        if (!cache.IsNoHistory(a))
        {
            batch.Add(oldDatom.With(EAVTHistory));
            batch.Add(retractDatom.With(EAVTHistory));
            
            batch.Add(oldDatom.With(AEVTHistory));
            batch.Add(retractDatom.With(AEVTHistory));
            
            if (isIndexed)
            {
                batch.Add(oldDatom.With(AVETHistory));
                batch.Add(retractDatom.With(AVETHistory));
            }
            if (isReference)
            {
                batch.Add(oldDatom.With(VAETHistory));
                batch.Add(retractDatom.With(VAETHistory));
            }
        }
    }
}
