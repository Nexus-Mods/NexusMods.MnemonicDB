using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB;

/// <summary>
/// This is a wrapper around snapshots that allows you to query the snapshot as of a specific transaction
/// id, this requires merging two indexes together, and then the deduplication of the merged index (retractions
/// removing assertions).
/// </summary>
internal class AsOfSnapshot(ISnapshot inner, TxId asOfTxId, AttributeRegistry registry) : ISnapshot
{
    /// <inheritdoc />
    public IEnumerable<Datom> Datoms(IndexType type, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var current = inner.Datoms(type.CurrentVariant(), a, b);
        var history = inner.Datoms(type.HistoryVariant(), a, b);
        var comparatorFn = type.GetComparator();
        var merged = current.Merge(history,
            (dCurrent, dHistory) => comparatorFn.CompareInstance(dCurrent.RawSpan, dHistory.RawSpan));
        var filtered = merged.Where(d => d.T <= asOfTxId);

        var withoutRetracts = ApplyRetracts(filtered);
        return withoutRetracts;
    }



    /// <summary>
    /// In a perfect situation this function can be fairly optimized. We simply take in a datom, but
    /// don't release it downstream until we check the next datom. If the next datom is a retraction
    /// then we skip the current datom and the retract.
    ///
    /// Naturally this all falls apart of retractions are not in the correct order. For example, if
    /// a three asserts for different attributes are passed in first, then the retract then there's
    /// a bit of a problem, but this never happens during normal usage of the database.
    ///
    /// In addition, if the iteration is happening in reverse order, then we need to keep track of the
    /// retraction first, then the assert.
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    public IEnumerable<Datom> ApplyRetracts(IEnumerable<Datom> src)
    {
        using var lastDatom = new PooledMemoryBufferWriter();
        var havePrevious = false;

        foreach (var entry in src)
        {
            if (!havePrevious)
            {
                lastDatom.Reset();
                lastDatom.Write(entry.RawSpan);
                havePrevious = true;
                continue;
            }

            var isRetract = IsRetractionFor(lastDatom.GetWrittenSpan(), entry.RawSpan);

            if (isRetract)
            {
                lastDatom.Reset();
                havePrevious = false;
                continue;
            }

            yield return new Datom(lastDatom.WrittenMemory, registry);
            lastDatom.Reset();
            lastDatom.Write(entry.RawSpan);
        }
        if (havePrevious)
        {
            yield return new Datom(lastDatom.WrittenMemory, registry);
        }
    }

    private bool IsRetractionFor(ReadOnlySpan<byte> aSpan, ReadOnlySpan<byte> bSpan)
    {
        var spanA = MemoryMarshal.Read<KeyPrefix>(aSpan);
        var spanB = MemoryMarshal.Read<KeyPrefix>(bSpan);

        // The lower bit of the lower 8 bytes is the retraction bit, the rest is the Entity ID
        // so if this XOR returns 1, we know they are the same Entity ID and one of them is a retraction
        if ((spanA.Lower ^ spanB.Lower) != 1)
        {
            return false;
        }

        // If the attribute is different, then it's not a retraction
        if (spanA.A != spanB.A)
        {
            return false;
        }

        // Retracts have to come after the asserts
        if (spanA.IsRetract && spanA.T < spanB.T)
        {
            return true;
        }

        return aSpan.SliceFast(KeyPrefix.Size).SequenceEqual(bSpan.SliceFast(KeyPrefix.Size));

    }
}
