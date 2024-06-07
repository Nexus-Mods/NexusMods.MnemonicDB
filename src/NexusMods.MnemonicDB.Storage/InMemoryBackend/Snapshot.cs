using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.Storage.InMemoryBackend;

public class Snapshot : ISnapshot
{
    private readonly ImmutableSortedSet<byte[]>[] _indexes;
    private readonly AttributeRegistry _registry;

    public Snapshot(ImmutableSortedSet<byte[]>[] indexes, AttributeRegistry registry)
    {
        _registry = registry;
        _indexes = indexes;
    }

    public void Dispose() { }

    /// <inheritdoc />
    public IndexSegment Datoms(SliceDescriptor descriptor)
    {
        var idxLower = _indexes[(int)descriptor.Index].IndexOf(descriptor.From.RawSpan.ToArray());
        var idxUpper = _indexes[(int)descriptor.Index].IndexOf(descriptor.To.RawSpan.ToArray());

        if (idxLower < 0)
            idxLower = ~idxLower;

        if (idxUpper < 0)
            idxUpper = ~idxUpper;

        var lower = idxLower;
        var upper = idxUpper;
        var reverse = false;

        if (idxLower > idxUpper)
        {
            lower = idxUpper;
            upper = idxLower;
            reverse = true;
        }

        using var segmentBuilder = new IndexSegmentBuilder();
        var index = _indexes[(int)descriptor.Index];

        if (!reverse)
        {
            for (var i = lower; i < upper; i++)
            {
                segmentBuilder.Add(index.ElementAt(i));
            }
        }
        else
        {
            for (var i = upper; i > lower; i--)
            {
                segmentBuilder.Add(index.ElementAt(i));
            }
        }
        return segmentBuilder.Build();
    }

    /// <inheritdoc />
    public IEnumerable<IndexSegment> DatomsChunked(SliceDescriptor descriptor, int chunkSize)
    {
        var idxLower = _indexes[(int)descriptor.Index].IndexOf(descriptor.From.RawSpan.ToArray());
        var idxUpper = _indexes[(int)descriptor.Index].IndexOf(descriptor.To.RawSpan.ToArray());

        if (idxLower < 0)
            idxLower = ~idxLower;

        if (idxUpper < 0)
            idxUpper = ~idxUpper;

        var lower = idxLower;
        var upper = idxUpper;
        var reverse = false;

        if (idxLower > idxUpper)
        {
            lower = idxUpper;
            upper = idxLower;
            reverse = true;
        }

        using var segmentBuilder = new IndexSegmentBuilder();
        var index = _indexes[(int)descriptor.Index];

        if (!reverse)
        {
            for (var i = lower; i < upper; i++)
            {
                segmentBuilder.Add(index.ElementAt(i));
                if (segmentBuilder.Count == chunkSize)
                {
                    yield return segmentBuilder.Build();
                    segmentBuilder.Reset();
                }
            }
        }
        else
        {
            for (var i = upper; i > lower; i--)
            {
                segmentBuilder.Add(index.ElementAt(i));
                if (segmentBuilder.Count == chunkSize)
                {
                    yield return segmentBuilder.Build();
                    segmentBuilder.Reset();
                }
            }
        }
        yield return segmentBuilder.Build();
    }
}
