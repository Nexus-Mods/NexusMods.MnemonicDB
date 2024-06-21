using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
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
    public IndexSegment Datoms(SliceDescriptor descriptor)
    {
        // TODO: stop using IEnumerable and use IndexSegment directly
        var current = inner.Datoms(descriptor with {Index = descriptor.Index.CurrentVariant()});
        var history = inner.Datoms(descriptor with {Index = descriptor.Index.HistoryVariant()});
        var comparatorFn = descriptor.Index.GetComparator();

        using var builder = new IndexSegmentBuilder(registry);

        var merged = current.Merge(history,
            (dCurrent, dHistory) => comparatorFn.CompareInstance(dCurrent, dHistory));
        var filtered = merged.Where(d => d.T <= asOfTxId);

        var withoutRetracts = ApplyRetracts(filtered);

        foreach (var datom in withoutRetracts)
        {
            builder.Add(datom);
        }

        return builder.Build();
    }

    public IEnumerable<IndexSegment> DatomsChunked(SliceDescriptor descriptor, int chunkSize)
    {
        // TODO: stop using IEnumerable and use IndexSegment directly
        var current = inner.DatomsChunked(descriptor with {Index = descriptor.Index.CurrentVariant()}, chunkSize).SelectMany(c => c);
        var history = inner.DatomsChunked(descriptor with {Index = descriptor.Index.HistoryVariant()}, chunkSize).SelectMany(c => c);
        var comparatorFn = descriptor.Index.GetComparator();

        using var builder = new IndexSegmentBuilder(registry);

        var merged = current.Merge(history,
            (dCurrent, dHistory) => comparatorFn.CompareInstance(dCurrent, dHistory));
        var filtered = merged.Where(d => d.T <= asOfTxId);

        var withoutRetracts = ApplyRetracts(filtered);

        foreach (var datom in withoutRetracts)
        {
            builder.Add(datom);
            if (builder.Count % chunkSize == 0)
            {
                yield return builder.Build();
                builder.Reset();
            }
        }

        yield return builder.Build();
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
                lastDatom.Write(entry);
                havePrevious = true;
                continue;
            }

            var isRetract = IsRetractionFor(lastDatom.GetWrittenSpan(), entry);

            if (isRetract)
            {
                lastDatom.Reset();
                havePrevious = false;
                continue;
            }

            yield return new Datom(lastDatom.WrittenMemory, registry);
            lastDatom.Reset();
            lastDatom.Write(entry);
        }
        if (havePrevious)
        {
            yield return new Datom(lastDatom.WrittenMemory, registry);
        }
    }

    private bool IsRetractionFor(ReadOnlySpan<byte> aSpan, Datom bDatom)
    {
        var spanA = MemoryMarshal.Read<KeyPrefix>(aSpan);

        // The lower bit of the lower 8 bytes is the retraction bit, the rest is the Entity ID
        // so if this XOR returns 1, we know they are the same Entity ID and one of them is a retraction
        if ((spanA.Lower ^ bDatom.Prefix.Lower) != 1)
        {
            return false;
        }

        // If the attribute is different, then it's not a retraction
        if (spanA.A != bDatom.A)
        {
            return false;
        }

        // Retracts have to come after the asserts
        if (spanA.IsRetract && spanA.T < bDatom.T)
        {
            return true;
        }

        if (spanA.ValueTag != bDatom.Prefix.ValueTag)
        {
            return false;
        }

        return aSpan.SliceFast(KeyPrefix.Size).SequenceEqual(bDatom.ValueSpan);

    }
}
