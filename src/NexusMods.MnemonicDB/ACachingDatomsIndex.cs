using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
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
            var builder = new IndexSegmentBuilder(attributeCache: parent.AttributeCache);
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

    public void BulkCache(EntityIds ids)
    {
        var numThreads = Environment.ProcessorCount;
        var chunkSize = ids.Count / numThreads;

        if (ids.Count == 0)
            return;
        
        if (ids.Count < 1024)
        {
            BulkCache(ids.Span);
            return;
        }
        
        var partioner = Partitioner.Create(0, ids.Count, chunkSize);

        Parallel.ForEach(partioner, range =>
        {
            BulkCache(ids.Span.Slice(range.Item1, range.Item2 - range.Item1));
        });

    }
    public void BulkCache(ReadOnlySpan<EntityId> ids)
    {
        if (ids.Length == 0)
            return;
        
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
                    if (eidIndex >= ids.Length)
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
