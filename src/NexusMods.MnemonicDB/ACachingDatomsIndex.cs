using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
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
    
    /// <summary>
    /// Bulk-loads and caches all the datoms attached to the given entity id via the given attribute. The provided
    /// attachedDb is used as the context db for the entities
    /// </summary>
    public Entities<TModel> GetBackrefModels<TModel>(IDb attachedDb, AttributeId aid, EntityId id)
        where TModel : IReadOnlyModel<TModel>
    {
        // Get the ids we need to load, these will be sorted by EntityId
        var ids = GetBackRefs(aid, id, out var cacheHit);
        
        // If we have a cache hit, no reason to bulk load the other entities
        if (true || cacheHit || ids.Count == 0)
            return new Entities<TModel>(ids, attachedDb);
        
        
        var idMin = ids[0];
        var idMax = ids[^1];
        var wasteRatio = (ulong)ids.Count / (idMax.Value - idMin.Value + 1);
        
        Console.WriteLine("{0}tx Bulk loading backrefs {1} for {2} from {3} to {4} with a waste ratio of {5} span of {6} for {7} entities", attachedDb.BasisTxId, typeof(TModel), id, idMin, idMax, wasteRatio, (idMax.Value - idMin.Value + 1), ids.Count);

        // If the ids are to spread out, we're not going to bulk load them 
        if (ids.Count > 100 && wasteRatio > 4)
        {
            return new Entities<TModel>(ids, attachedDb);
        }

        // Now we are going to bulk load and cache the entities. We make an assumption here that a
        // seek in RocksDB is going to be way slower than a single iteration over a large number of 
        // mostly useless datoms. So we walk the entity ids in hand with the EAVT index and do a sorted
        // merge join of the two sets.

        // We're going to re-use this builder for each entity
        using var builder = new IndexSegmentBuilder(AttributeCache);
        var slice = SliceDescriptor.Create(ids[0], ids[^1]);
        
        // One enumerator for the entire entity segment
        using var enumerator = GetRefDatomEnumerator();

        var eidIndex = 0;
        var readingE = EntityId.MinValueNoPartition;
        var currentId = ids[eidIndex];
        while (enumerator.MoveNext(slice))
        {
            // New Entity, so cache what we have so far if we have anything
            if (enumerator.KeyPrefix.E != readingE)
            {
                if (builder.Count != 0)
                {
                    EntityCache.AddValue(readingE, AVSegment.Build(builder));
                    builder.Reset();
                }
                readingE = enumerator.KeyPrefix.E;
                if (currentId < readingE)
                {
                    eidIndex++;

                    // If we're out of range for the ids, we have all we need
                    if (eidIndex >= ids.Count)
                        break;

                    currentId = ids[eidIndex];
                }
            }
            
            // If we don't want this entity, skip it
            if (currentId > readingE)
                continue;
            
            // otherwise, add it to the builder
            builder.AddCurrent(enumerator);
        }

        // If we have extra stuff left in the buffer, make sure to cache it
        if (builder.Count != 0)
        {
            EntityCache.AddValue(readingE, AVSegment.Build(builder));
        }
        
        // From now on, getting a model in this set should be a cache hit
        return new Entities<TModel>(ids, attachedDb);
    }
}
