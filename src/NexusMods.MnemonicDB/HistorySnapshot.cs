using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB;

/// <summary>
/// This is a wrapper around snapshots that allows you to query the snapshot as of a specific transaction
/// id, this requires merging two indexes together, and then the deduplication of the merged index (retractions
/// removing assertions).
/// </summary>
internal class HistorySnapshot(ISnapshot inner, AttributeCache attributeCache) : ISnapshot
{
    public IndexSegment Datoms(SliceDescriptor descriptor)
    {
        // TODO: stop using IEnumerable and use IndexSegment directly
        var current = inner.Datoms(descriptor.WithIndex(descriptor.Index.CurrentVariant()));
        var history = inner.Datoms(descriptor.WithIndex(descriptor.Index.HistoryVariant()));
        var comparatorFn = descriptor.Index.GetComparator();

        using var builder = new IndexSegmentBuilder(attributeCache);

        var merged = current.Merge(history,
            (dCurrent, dHistory) => comparatorFn.CompareInstance(dCurrent, dHistory));

        foreach (var datom in merged)
        {
            builder.Add(datom);
        }

        return builder.Build();
    }

    public IEnumerable<IndexSegment> DatomsChunked(SliceDescriptor descriptor, int chunkSize)
    {
        // TODO: stop using IEnumerable and use IndexSegment directly
        var current = inner.DatomsChunked(descriptor.WithIndex(descriptor.Index.CurrentVariant()), chunkSize).SelectMany(c => c);
        var history = inner.DatomsChunked(descriptor.WithIndex(descriptor.Index.HistoryVariant()), chunkSize).SelectMany(c => c);
        var comparatorFn = descriptor.Index.GetComparator();

        using var builder = new IndexSegmentBuilder(attributeCache);

        var merged = current.Merge(history,
            (dCurrent, dHistory) => comparatorFn.CompareInstance(dCurrent, dHistory));

        foreach (var datom in merged)
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

    public IEnumerable<RefDatom> RefDatoms(SliceDescriptor descriptor)
    {
        throw new System.NotImplementedException();
    }
}
