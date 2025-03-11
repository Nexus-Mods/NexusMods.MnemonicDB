using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Caching;

namespace NexusMods.MnemonicDB;

/// <summary>
/// A wrapper for a datoms index that caches several index segment types
/// </summary>
public abstract class ACachingDatomsIndex<TRefEnumerator> : 
    ADatomsIndex<TRefEnumerator>
    where TRefEnumerator : IRefDatomEnumerator
{
    protected ACachingDatomsIndex(ACachingDatomsIndex<TRefEnumerator> other, IndexSegment addedDatoms) : base(other.AttributeCache)
    {
        EntityCache = other.EntityCache.Fork(addedDatoms, new EntityCacheStrategy(this));
        BackReferenceCache = other.BackReferenceCache.Fork(addedDatoms, new BackReferenceCacheStrategy(this));
    }

    /// <summary>
    /// A wrapper for a datoms index that caches several index segment types
    /// </summary>
    protected ACachingDatomsIndex(AttributeCache attributeCache) : base(attributeCache)
    {
        EntityCache = new IndexSegmentCache<EntityId, EntitySegment>(new EntityCacheStrategy(this));
        BackReferenceCache = new IndexSegmentCache<(AttributeId A, EntityId E), EntityIds>(new BackReferenceCacheStrategy(this));
    }

    private class EntityCacheStrategy(ACachingDatomsIndex<TRefEnumerator> parent) : CacheStrategy<EntityId, EntitySegment>
    {
        public override Memory<byte> GetBytes(EntityId key)
        {
            var builder = new IndexSegmentBuilder(parent.AttributeCache);
            using var iterator = parent.GetRefDatomEnumerator();
            builder.AddRange(iterator, SliceDescriptor.Create(key));
            return AVSegment.Build(builder);
        }

        public override EntitySegment GetValue(EntityId key, IDb db, Memory<byte> bytes)
        {
            return new EntitySegment(key, new AVSegment(bytes), db);
        }

        public override IEnumerable<EntityId> GetKeysFromRecentlyAdded(IndexSegment segment)
        {
            foreach (var datom in segment)
            {
                yield return datom.E;
            }
        }
    }
    
    private class BackReferenceCacheStrategy(ACachingDatomsIndex<TRefEnumerator> parent) : CacheStrategy<(AttributeId A, EntityId E), EntityIds>
    {
        public override Memory<byte> GetBytes((AttributeId A, EntityId E) key)
        {
            var builder = new IndexSegmentBuilder(parent.AttributeCache);
            using var iterator = parent.GetRefDatomEnumerator();
            builder.AddRange(iterator, SliceDescriptor.Create(key.A, key.E));
            return EntityIds.Build(builder);
        }

        public override EntityIds GetValue((AttributeId A, EntityId E) key, IDb db, Memory<byte> bytes)
        {
            return new EntityIds { Data = bytes };
        }

        public override IEnumerable<(AttributeId A, EntityId E)> GetKeysFromRecentlyAdded(IndexSegment segment)
        {
            foreach (var datom in segment)
            {
                if (datom.Prefix.ValueTag == ValueTag.Reference)
                {
                    var eVal = MemoryMarshal.Read<EntityId>(datom.ValueSpan);
                    yield return (datom.A, eVal);
                }
            }
        }
    }

    private IndexSegmentCache<EntityId, EntitySegment> EntityCache { get; }

    private IndexSegmentCache<(AttributeId A, EntityId E), EntityIds> BackReferenceCache { get; }

    /// <inheritdoc />
    public override EntitySegment GetEntitySegment(IDb db, EntityId entityId)
        => EntityCache.GetValue(entityId, db);

    
    /// <inheritdoc />
    public override EntityIds GetEntityIdsPointingTo(AttributeId attrId, EntityId entityId) 
        => BackReferenceCache.GetValue((attrId, entityId), null!);
        
}
