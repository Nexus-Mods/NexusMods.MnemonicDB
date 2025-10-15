using System;
using System.Buffers;
using System.Collections.Generic;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.Traits;

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
    public IReadOnlyList<IDatomLikeRO> Datoms<TDescriptor>(TDescriptor descriptor) where TDescriptor : ISliceDescriptor
    {
        using var builder = new IndexSegmentBuilder(AttributeCache);
        using var iterator = GetRefDatomEnumerator(descriptor is SliceDescriptor);
        var result = new List<IDatomLikeRO>();
        while (iterator.MoveNext(descriptor))
        {
            result.Add(ValueDatom.Create(iterator));
        }
        return result;
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

    /// <summary>
    /// A lightweight datom segment doesn't load the entire set into memory.
    /// </summary>
    [MustDisposeResource]
    public ILightweightDatomSegment LightweightDatoms<TDescriptor>(TDescriptor descriptor, bool totalOrdered = false)
        where TDescriptor : ISliceDescriptor
    {
        var enumerator = GetRefDatomEnumerator(totalOrdered);
        return new LightweightDatomSegment<TRefEnumerator, TDescriptor>(enumerator, descriptor);
    }

    public int IdsForPrimaryAttribute(AttributeId attributeId, int chunkSize, out List<EntityId[]> chunks)
    {
        List<EntityId[]> result = [];
        using var iterator = GetRefDatomEnumerator();
        var slice = SliceDescriptor.Create(attributeId);
        var chunk = ArrayPool<EntityId>.Shared.Rent(chunkSize);
        result.Add(chunk);
        int chunkOffset = 0;
        while (iterator.MoveNext(slice))
        {
            chunk[chunkOffset] = iterator.E;
            chunkOffset++;
            if (chunkOffset >= chunk.Length)
            {
                chunk = ArrayPool<EntityId>.Shared.Rent(chunkSize);
                chunkOffset = 0;
                result.Add(chunk);
            }
        }

        chunks = result;
        return (result.Count - 1) * chunkSize + chunkOffset;
    }
    
    
    public virtual EntitySegment GetEntitySegment(IDb db, EntityId entityId)
    {
        using var builder = new IndexSegmentBuilder(AttributeCache);
        using var iterator = GetRefDatomEnumerator();
        builder.AddRange(iterator, SliceDescriptor.Create(entityId));
        var avSegment = AVSegment.Build(builder);
        return new EntitySegment(entityId, new AVSegment(avSegment), db);
    }

    public virtual EntityIds GetEntityIdsPointingTo(AttributeId attrId, EntityId entityId)
    {
        var slice = SliceDescriptor.Create(attrId, entityId);
        using var builder = new IndexSegmentBuilder(AttributeCache);
        using var iterator = GetRefDatomEnumerator();
        builder.AddRange(iterator, slice);
        return new EntityIds { Data = EntityIds.Build(builder) };
    }
    
    public virtual EntityIds GetBackRefs(AttributeId attribute, EntityId id)
    {
        return GetEntityIdsPointingTo(attribute, id);
    }

    public IndexSegment ReferencesTo(EntityId eid)
    {
        using var builder = new IndexSegmentBuilder(AttributeCache);
        using var iterator = GetRefDatomEnumerator();
        builder.AddRange(iterator, SliceDescriptor.CreateReferenceTo(eid));
        return builder.Build();
    }

    public IReadOnlyList<IDatomLikeRO> Datoms(TxId txId)
    {
        using var iterator = GetRefDatomEnumerator();
        var slice = SliceDescriptor.Create(txId);
        var list = new List<IDatomLikeRO>();
        while (iterator.MoveNext(slice))
            list.Add(ValueDatom.Create(iterator));
        return list;
    }

    /// <inheritdoc />
    public abstract TRefEnumerator GetRefDatomEnumerator(bool totalOrder = false);
}
