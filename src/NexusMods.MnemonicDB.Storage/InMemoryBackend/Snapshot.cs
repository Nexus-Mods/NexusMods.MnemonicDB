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
        var thisIndex = _indexes[(int)descriptor.Index];
        if (thisIndex.Count == 0)
            return new IndexSegment();

        var idxLower = thisIndex.IndexOf(descriptor.From.ToArray());
        var idxUpper = thisIndex.IndexOf(descriptor.To.ToArray());
        bool upperExact = true;
        bool lowerExact = true;

        if (idxLower < 0)
        {
            idxLower = ~idxLower;
            lowerExact = false;
        }

        if (idxUpper < 0)
        {
            idxUpper = ~idxUpper;
            upperExact = false;
        }

        var lower = idxLower;
        var upper = idxUpper;

        if (descriptor.IsReverse)
        {
            lower = idxUpper;
            upper = idxLower;
            (lowerExact, upperExact) = (upperExact, lowerExact);
        }

        using var segmentBuilder = new IndexSegmentBuilder(_registry);

        if (descriptor.IsReverse)
        {
            if (!lowerExact)
                lower++;
            for (var i = upper; i >= lower; i--)
            {
                segmentBuilder.Add(thisIndex.ElementAt(i));
            }
        }
        else
        {
            if (!upperExact)
                upper--;
            for (var i = lower; i <= upper; i++)
            {
                segmentBuilder.Add(thisIndex.ElementAt(i));
            }
        }

        return segmentBuilder.Build();
    }

    /// <inheritdoc />
    public IEnumerable<IndexSegment> DatomsChunked(SliceDescriptor descriptor, int chunkSize)
    {
        var idxLower = _indexes[(int)descriptor.Index].IndexOf(descriptor.From.ToArray());
        var idxUpper = _indexes[(int)descriptor.Index].IndexOf(descriptor.To.ToArray());

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

        using var segmentBuilder = new IndexSegmentBuilder(_registry);
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
