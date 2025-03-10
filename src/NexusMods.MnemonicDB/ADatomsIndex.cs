using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB;

public abstract class ADatomsIndex<TRefEnumerator> : IDatomsIndex, IRefDatomEnumeratorFactory<TRefEnumerator>
    where TRefEnumerator : IRefDatomEnumerator
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
        using var iterator = GetRefDatomEnumerator();
        builder.AddRange(iterator, descriptor);
        return builder.Build();
    }
    
    public IEnumerable<IndexSegment> DatomsChunked<TSliceDescriptor>(TSliceDescriptor descriptor, int chunkSize) 
        where TSliceDescriptor : ISliceDescriptor
    {
        using var builder = new IndexSegmentBuilder(AttributeCache);
        using var iterator = GetRefDatomEnumerator();
        while (iterator.MoveNext(descriptor))
        {
            builder.AddCurrent(iterator);
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
        using var iterator = GetRefDatomEnumerator();
        builder.AddRange(iterator, SliceDescriptor.Create(entityId));
        var avSegment = AVSegment.Build(builder);
        return new EntitySegment(entityId, new AVSegment() { Data = avSegment }, db);
    }

    public EntityIds GetEntityIdsPointingTo(AttributeId attrId, EntityId entityId)
    {
        var slice = SliceDescriptor.Create(attrId, entityId);
        using var builder = new IndexSegmentBuilder(AttributeCache);
        using var iterator = GetRefDatomEnumerator();
        builder.AddRange(iterator, slice);
        return new EntityIds(builder.BuildEntityIds());
    }
    
    public EntityIds GetBackRefs(ReferenceAttribute attribute, EntityId id)
    {
        var attrId = AttributeCache.GetAttributeId(attribute.Id);
        return GetEntityIdsPointingTo(attrId, id);
    }

    public IndexSegment ReferencesTo(EntityId eid)
    {
        using var builder = new IndexSegmentBuilder(AttributeCache);
        using var iterator = GetRefDatomEnumerator();
        builder.AddRange(iterator, SliceDescriptor.CreateReferenceTo(eid));
        return builder.Build();
    }

    /// <inheritdoc />
    public abstract TRefEnumerator GetRefDatomEnumerator();
}
