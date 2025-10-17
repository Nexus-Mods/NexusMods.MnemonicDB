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
    protected ACachingDatomsIndex(ACachingDatomsIndex<TRefEnumerator> other, DatomList addedDatoms) : base(other.AttributeCache)
    {
        EntityCache = other.EntityCache.Fork(addedDatoms, new EntityCacheStrategy(this));
        BackReferenceCache = other.BackReferenceCache.Fork(addedDatoms, new BackReferenceCacheStrategy(this));
    }

    /// <summary>
    /// A wrapper for a datoms index that caches several index segment types
    /// </summary>
    protected ACachingDatomsIndex(AttributeCache attributeCache) : base(attributeCache)
    {
        EntityCache = new IndexSegmentCache<EntityId>(new EntityCacheStrategy(this));
        BackReferenceCache = new IndexSegmentCache<(AttributeId A, EntityId E)>(new BackReferenceCacheStrategy(this));
    }

    private class EntityCacheStrategy(ACachingDatomsIndex<TRefEnumerator> parent) : CacheStrategy<EntityId>
    {
        public override DatomList GetDatoms(EntityId key)
        {
            var datoms = new DatomList(parent.AttributeCache);
            using var iterator = parent.GetRefDatomEnumerator();
            datoms.Add(iterator, SliceDescriptor.Create(key));
            return datoms;
        }
        public override IEnumerable<EntityId> GetKeysFromRecentlyAdded(DatomList segment)
        {
            foreach (var datom in segment)
            {
                yield return datom.E;
            }
        }
    }
    
    private class BackReferenceCacheStrategy(ACachingDatomsIndex<TRefEnumerator> parent) : CacheStrategy<(AttributeId A, EntityId E)>
    {
        public override DatomList GetDatoms((AttributeId A, EntityId E) key)
        {
            var datoms = new DatomList(parent.AttributeCache);
            using var iterator = parent.GetRefDatomEnumerator();
            datoms.Add(iterator, SliceDescriptor.Create(key.A, key.E));
            return datoms;
        }

        public override IEnumerable<(AttributeId A, EntityId E)> GetKeysFromRecentlyAdded(DatomList segment)
        {
            return segment
                .Where(static d => d.Value is EntityId)
                .Select(static d => (d.A, (EntityId)d.Value));
        }
    }

    public IndexSegmentCache<EntityId> EntityCache { get; }

    public IndexSegmentCache<(AttributeId A, EntityId E)> BackReferenceCache { get; }

    public void BulkCache(EntityIds ids)
    {
        throw new NotImplementedException();

    }
    public void BulkCache(ReadOnlySpan<EntityId> ids)
    {
        throw new NotImplementedException();
    }
}
