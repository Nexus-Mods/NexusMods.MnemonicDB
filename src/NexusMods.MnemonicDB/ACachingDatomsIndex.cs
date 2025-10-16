using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.Traits;
using NexusMods.MnemonicDB.Caching;

namespace NexusMods.MnemonicDB;

/// <summary>
/// A wrapper for a datoms index that caches several index segment types
/// </summary>
public abstract class ACachingDatomsIndex<TRefEnumerator> : 
    ADatomsIndex<TRefEnumerator>
    where TRefEnumerator : IRefDatomEnumerator
{
    protected ACachingDatomsIndex(ACachingDatomsIndex<TRefEnumerator> other, IReadOnlyList<IDatomLikeRO> addedDatoms) : base(other.AttributeCache)
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
            throw new NotImplementedException();
            //return AVSegment.Build(builder);
        }

        public override EntitySegment GetValue(EntityId key, IDb db, Memory<byte> bytes)
        {
            throw new NotImplementedException();
            //return new EntitySegment(key, new AVSegment(bytes), db);
        }

        public override IEnumerable<EntityId> GetKeysFromRecentlyAdded(IReadOnlyList<IDatomLikeRO> segment)
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

        public override IEnumerable<(AttributeId A, EntityId E)> GetKeysFromRecentlyAdded(IReadOnlyList<IDatomLikeRO> segment)
        {
            return segment
                .Where(static d => d.Value is EntityId)
                .Select(static d => (d.A, (EntityId)d.Value));
        }
    }

    public IndexSegmentCache<EntityId, EntitySegment> EntityCache { get; }

    public IndexSegmentCache<(AttributeId A, EntityId E), EntityIds> BackReferenceCache { get; }

    /// <inheritdoc />
    public override EntitySegment GetEntitySegment(IDb db, EntityId entityId)
        => EntityCache.GetValue(entityId, db, out _);

    
    /// <inheritdoc />
    public override EntityIds GetBackRefs(AttributeId attrId, EntityId entityId) 
        => BackReferenceCache.GetValue((attrId, entityId), null!, out _);
    
    /// <inheritdoc />
    public EntityIds GetBackRefs(AttributeId attrId, EntityId entityId, out bool cacheHit) 
        => BackReferenceCache.GetValue((attrId, entityId), null!, out cacheHit);

    public void BulkCache(EntityIds ids)
    {
        throw new NotImplementedException();

    }
    public void BulkCache(ReadOnlySpan<EntityId> ids)
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Converts the ids into a list of models. In the future this method may be expanded to auto-cache
    /// entities based on some heuristic, but for now it just loads the ids.
    /// </summary>
    public Entities<TModel> GetBackrefModels<TModel>(IDb attachedDb, AttributeId aid, EntityId id)
        where TModel : IReadOnlyModel<TModel>
    {
        // Get the ids we need to load, these will be sorted by EntityId
        var ids = GetBackRefs(aid, id, out var cacheHit);
        return new Entities<TModel>(ids, attachedDb);
    }
}
