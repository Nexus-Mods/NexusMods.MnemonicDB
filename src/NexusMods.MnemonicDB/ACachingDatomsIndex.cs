using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
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
    protected ACachingDatomsIndex(ACachingDatomsIndex<TRefEnumerator> other, Datoms addedDatoms) : base(other.AttributeCache)
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
        public override Datoms GetDatoms(EntityId key)
        {
            var datoms = new Datoms(parent.AttributeCache);
            using var iterator = parent.GetRefDatomEnumerator();
            datoms.Add(iterator, SliceDescriptor.Create(key));
            return datoms;
        }
        public override IEnumerable<EntityId> GetKeysFromRecentlyAdded(Datoms segment)
        {
            foreach (var datom in segment)
            {
                yield return datom.E;
            }
        }
    }
    
    private class BackReferenceCacheStrategy(ACachingDatomsIndex<TRefEnumerator> parent) : CacheStrategy<(AttributeId A, EntityId E)>
    {
        public override Datoms GetDatoms((AttributeId A, EntityId E) key)
        {
            var datoms = new Datoms(parent.AttributeCache);
            using var iterator = parent.GetRefDatomEnumerator();
            datoms.Add(iterator, SliceDescriptor.Create(key.A, key.E));
            return datoms;
        }

        public override IEnumerable<(AttributeId A, EntityId E)> GetKeysFromRecentlyAdded(Datoms segment)
        {
            return segment
                .Where(static d => d.Value is EntityId)
                .Select(static d => (d.A, (EntityId)d.Value));
        }
    }

    public IndexSegmentCache<EntityId> EntityCache { get; }

    public IndexSegmentCache<(AttributeId A, EntityId E)> BackReferenceCache { get; }
}
