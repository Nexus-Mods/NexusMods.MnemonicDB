using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB;

public abstract class ADatomsIndex<TLowLevelIterator> : IDatomsIndex, ILowLevelIteratorFactory<TLowLevelIterator>
    where TLowLevelIterator : ILowLevelIterator
{
    protected ADatomsIndex(AttributeCache cache)
    {
        AttributeCache = cache;
    }
    public AttributeCache AttributeCache { get; }

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

    public EntitySegment GetEntitySegment(IDb db, EntityId entityId)
    {
        using var builder = new IndexSegmentBuilder(AttributeCache);
        using var iterator = GetLowLevelIterator();
        builder.AddRange(iterator.Slice(SliceDescriptor.Create(entityId)));
        return builder.BuildEntitySegment(db, entityId);
    }

    public EntityIds GetEntityIdsPointingTo(AttributeId attrId, EntityId entityId)
    {
        var slice = SliceDescriptor.Create(attrId, entityId);
        using var builder = new IndexSegmentBuilder(AttributeCache);
        using var iterator = GetLowLevelIterator();
        builder.AddRange(iterator.Slice(slice));
        return new EntityIds(builder.BuildEntityIds());
    }

    /// <inheritdoc />
    public abstract TLowLevelIterator GetLowLevelIterator();
}
