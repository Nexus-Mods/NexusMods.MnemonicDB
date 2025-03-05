using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB;

public abstract class ADatomsIndex<TLowLevelIterator> : IDatomsIndex
    where TLowLevelIterator : ILowLevelIterator
{
    protected ADatomsIndex(AttributeCache cache)
    {
        AttributeCache = cache;
    }
    public AttributeCache AttributeCache { get; }
    /// <summary>
    /// Get a low-level iterator for the backing store
    /// </summary>
    protected abstract TLowLevelIterator GetLowLevelIterator();

    /// <summary>
    /// Get datoms for a specific descriptor
    /// </summary>
    public IndexSegment Datoms<TDescriptor>(TDescriptor descriptor) where TDescriptor : ISliceDescriptor
    {
        using var builder = new IndexSegmentBuilder(AttributeCache);
        using var iterator = GetLowLevelIterator();
        builder.AddRange(iterator.Slice(descriptor));
        return builder.Build();
    }
    
    public IEnumerable<IndexSegment> DatomsChunked<TSliceDescriptor>(TSliceDescriptor descriptor, int chunkSize) 
        where TSliceDescriptor : ISliceDescriptor
    {
        using var builder = new IndexSegmentBuilder(AttributeCache);
        using var iterator = GetLowLevelIterator();
        var slice = iterator.Slice(descriptor);
        while (slice.MoveNext())
        {
            builder.AddCurrent(slice);
            if (builder.Count == chunkSize)
            {
                yield return builder.Build();
                builder.Reset();
            }
        }
        if (builder.Count > 0)
            yield return builder.Build();
    }
}
