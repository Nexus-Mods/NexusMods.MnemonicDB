using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.Storage.InMemoryBackend;

public class Snapshot : ISnapshot
{
    private readonly ImmutableSortedSet<byte[]>[] _indexes;
    private readonly AttributeCache _attributeCache;

    public Snapshot(ImmutableSortedSet<byte[]>[] indexes, AttributeCache attributeCache)
    {
        _attributeCache = attributeCache;
        _indexes = indexes;
    }

    public void Dispose() { }

    /// <inheritdoc />
    public IndexSegment Datoms(SliceDescriptor descriptor)
    {
        var index = _indexes[(int)descriptor.Index];
        var isReverse = descriptor.IsReverse;
        int increment = 1;
        int startIndex;

        if (!isReverse)
        {
            var indexOf = index.IndexOf(descriptor.From.ToArray());
            if (indexOf >= 0)
                startIndex = indexOf;
            else
                startIndex = ~indexOf;
        }
        else
        {
            increment = -1;
            var indexOf = index.IndexOf(descriptor.From.ToArray());
            if (indexOf >= 0)
                startIndex = indexOf;
            else
                startIndex = (~indexOf) - 1;
        }
        
        using var segmentBuilder = new IndexSegmentBuilder(_attributeCache);

        while (true)
        {
            if (startIndex < 0 || startIndex >= index.Count)
                break;
            
            var current = index.ElementAt(startIndex);
            var datom = new Datom(current);
            if (!descriptor.Includes(in datom))
                break;
            
            segmentBuilder.Add(current);
            startIndex += increment;

        } 
        return segmentBuilder.Build();
    }

    /// <inheritdoc />
    public IEnumerable<IndexSegment> DatomsChunked(SliceDescriptor descriptor, int chunkSize)
    {
        var index = _indexes[(int)descriptor.Index];
        var isReverse = descriptor.IsReverse;
        var includesDescriptor = descriptor;
        int increment = 1;
        int startIndex;

        if (!isReverse)
        {
            var indexOf = index.IndexOf(descriptor.From.ToArray());
            if (indexOf >= 0)
                startIndex = indexOf;
            else
                startIndex = ~indexOf;
        }
        else
        {
            includesDescriptor = descriptor.Reversed();
            increment = -1;
            var indexOf = index.IndexOf(descriptor.From.ToArray());
            if (indexOf >= 0)
                startIndex = indexOf;
            else
                startIndex = (~indexOf) - 1;
        }
        
        using var segmentBuilder = new IndexSegmentBuilder(_attributeCache);

        while (true)
        {
            if (startIndex < 0 || startIndex >= index.Count)
                break;
            
            var current = index.ElementAt(startIndex);
            var datom = new Datom(current);
            if (!includesDescriptor.Includes(in datom))
                break;
            
            segmentBuilder.Add(current);
            startIndex += increment;
            
            if (segmentBuilder.Count == chunkSize)
            {
                yield return segmentBuilder.Build();
                segmentBuilder.Reset();
            }
        }
        if (segmentBuilder.Count > 0) 
            yield return segmentBuilder.Build();
    }
}
