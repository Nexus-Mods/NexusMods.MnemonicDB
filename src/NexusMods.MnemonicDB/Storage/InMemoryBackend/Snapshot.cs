using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage.Abstractions;

namespace NexusMods.MnemonicDB.Storage.InMemoryBackend;

using IndexData = ImmutableSortedSet<byte[]>;

public class Snapshot : ISnapshot
{
    private readonly IndexData _index;
    private readonly AttributeCache _attributeCache;

    public Snapshot(IndexData index, AttributeCache attributeCache)
    {
        _attributeCache = attributeCache;
        _index = index;
    }

    public void Dispose() { }

    /// <inheritdoc />
    public IndexSegment Datoms(SliceDescriptor descriptor)
    {
        var isReverse = descriptor.IsReverse;
        var increment = 1;
        int startIndex;
        
        var fromIDatom = new IndexDatom
        {
            Index = descriptor.Index,
            Datom = descriptor.From
        };
        
        var indexOf = _index.IndexOf(fromIDatom.ToArray());
        if (!isReverse)
        {
            if (indexOf >= 0)
                startIndex = indexOf;
            else
                startIndex = ~indexOf;
        }
        else
        {
            increment = -1;
            if (indexOf >= 0)
                startIndex = indexOf;
            else
                startIndex = (~indexOf) - 1;
        }
        
        using var segmentBuilder = new IndexSegmentBuilder(_attributeCache);

        while (true)
        {
            if (startIndex < 0 || startIndex >= _index.Count)
                break;
            
            var current = _index.ElementAt(startIndex);
            var datom = new Datom(current.AsMemory()[1..]);
            if ((IndexType)current[0] != descriptor.Index || !descriptor.Includes(in datom))
                break;
            
            segmentBuilder.Add(datom);
            startIndex += increment;

        } 
        return segmentBuilder.Build();
    }

    /// <inheritdoc />
    public IEnumerable<IndexSegment> DatomsChunked(SliceDescriptor descriptor, int chunkSize)
    {
        var isReverse = descriptor.IsReverse;
        var includesDescriptor = descriptor;
        var increment = 1;
        int startIndex;
        
        var fromIDatom = new IndexDatom
        {
            Index = descriptor.Index,
            Datom = descriptor.From
        };
        
        var indexOf = _index.IndexOf(fromIDatom.ToArray());
        if (!isReverse)
        {
            if (indexOf >= 0)
                startIndex = indexOf;
            else
                startIndex = ~indexOf;
        }
        else
        {
            increment = -1;
            if (indexOf >= 0)
                startIndex = indexOf;
            else
                startIndex = (~indexOf) - 1;
        }
        
        using var segmentBuilder = new IndexSegmentBuilder(_attributeCache);

        while (true)
        {
            if (startIndex < 0 || startIndex >= _index.Count)
                break;
            
            var current = _index.ElementAt(startIndex);
            var datom = new Datom(current.AsMemory()[1..]);
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
