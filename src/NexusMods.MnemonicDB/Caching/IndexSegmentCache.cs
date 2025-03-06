using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Caching;

/// <summary>
/// A struct that holds enough information to uniquely identify a cache entry.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 12)]
public readonly struct CacheKey : IEquatable<CacheKey>
{
    /// <summary>
    /// The type of index that the cache entry is for.
    /// </summary>
    [FieldOffset(0)]
    public readonly IndexType IndexType;
    
    /// <summary>
    /// The attribute id that the cache entry is for.
    /// </summary>
    [FieldOffset(1)]
    public readonly AttributeId AttributeId;
    
    /// <summary>
    /// The entity id that the cache entry is for.
    /// </summary>
    [FieldOffset(4)]
    public readonly EntityId EntityId;
    
    /// <summary>
    /// Create a new cache key.
    /// </summary>
    private CacheKey(IndexType indexType, AttributeId attributeId, EntityId entityId)
    {
        IndexType = indexType;
        AttributeId = attributeId;
        EntityId = entityId;
    }
    
    /// <summary>
    /// Create a new cache key for the given index type, attribute id and entity id.
    /// </summary>
    public static CacheKey Create(IndexType indexType, AttributeId attributeId, EntityId entityId) => new(indexType, attributeId, entityId);
    
    /// <summary>
    /// Create a new cache key for the given index type and attribute id.
    /// </summary>
    public static CacheKey Create(IndexType indexType, EntityId entityId) => new(indexType, AttributeId.From(0), entityId);

    /// <inheritdoc />
    public bool Equals(CacheKey other)
    {
        return IndexType == other.IndexType && AttributeId.Equals(other.AttributeId) && EntityId.Equals(other.EntityId);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CacheKey other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine((int)IndexType, AttributeId, EntityId);
    }
}

/// <summary>
/// The value of a cache entry.
/// </summary>
public struct CacheValue : IEquatable<CacheValue>
{
    /// <summary>
    /// The last time the cache entry was accessed.
    /// </summary>
    public long LastAccessed;
    
    /// <summary>
    /// The cached index segment.
    /// </summary>
    public readonly object Segment;
    
    /// <summary>
    /// Create a new cache value.
    /// </summary>
    /// <param name="segment"></param>
    public CacheValue(object segment)
    {
        LastAccessed = CreateLastAccessed();
        Segment = segment;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long CreateLastAccessed(TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;
        return timeProvider.GetTimestamp();
    }

    /// <summary>
    /// Update the last accessed time to now.
    /// </summary>
    public void Hit()
    {
        LastAccessed = CreateLastAccessed();
    }

    /// <inheritdoc />
    public bool Equals(CacheValue other)
    {
        return LastAccessed == other.LastAccessed && Segment.Equals(other.Segment);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CacheValue other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(LastAccessed, Segment);
    }
}

/// <summary>
/// Immutable cache root
/// </summary>
public class CacheRoot
{
    private readonly ImmutableDictionary<CacheKey, CacheValue> _entries;

    private CacheRoot()
    {
        throw new NotSupportedException();
    }
    
    internal CacheRoot(ImmutableDictionary<CacheKey, CacheValue> entries)
    {
        _entries = entries;
    }
    
    /// <summary>
    /// Get the index segment for the given key, if it exists.
    /// </summary>
    public bool TryGetValue(CacheKey key, out object segment)
    {
        if (_entries.TryGetValue(key, out var value))
        {
            value.Hit();
            segment = value.Segment;
            return true;
        }
        segment = null!;
        return false;
    }

    /// <summary>
    /// Create a new cache root with the given segment added.
    /// </summary>
    public CacheRoot With(CacheKey key, object segment, IndexSegmentCache cache)
    {
        var newEntries = _entries.SetItem(key, new CacheValue(segment));
        if (newEntries.Count > cache.EntryCapacity)
        {
            newEntries = PurgeEntries(newEntries, newEntries.Count / 10);
        }
        
        return new CacheRoot(newEntries);
    }

    /// <summary>
    /// Purge the `toPurge` oldest entries from the cache.
    /// </summary>
    private ImmutableDictionary<CacheKey,CacheValue> PurgeEntries(ImmutableDictionary<CacheKey,CacheValue> newEntries, int toPurge)
    {
        var toDrop = newEntries.OrderBy(kv => kv.Value.LastAccessed).Take(toPurge);
        
        var builder = newEntries.ToBuilder();
        foreach (var kv in toDrop)
        {
            builder.Remove(kv.Key);
        }
        
        return builder.ToImmutable();
    }

    /// <summary>
    /// Evict cache entries for datoms in the given transaction log.
    /// </summary>
    public CacheRoot EvictNew(StoreResult result, out IndexSegment newDatoms)
    {
        newDatoms = result.Snapshot.Datoms(SliceDescriptor.Create(result.AssignedTxId));
        
        var editable = _entries.ToBuilder();
        foreach (var datom in newDatoms)
        {
            var eavtKey = CacheKey.Create(IndexType.EAVTCurrent, datom.E);
            editable.Remove(eavtKey);
            
            if (datom.Prefix.ValueTag != ValueTag.Reference)
                continue;
            
            var vaetKey = CacheKey.Create(IndexType.VAETCurrent, datom.A, MemoryMarshal.Read<EntityId>(datom.ValueSpan));
            editable.Remove(vaetKey);
            
            var referencesKey = CacheKey.Create(IndexType.VAETCurrent, MemoryMarshal.Read<EntityId>(datom.ValueSpan));
            editable.Remove(referencesKey);
        }
        return new(editable.ToImmutable());
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"CacheRoot: {_entries.Count} entries,";
    }
}

/// <summary>
/// A cache of index segments, implemented as an immutable LRU cache, immutable so that each subsequent
/// Db instance can reuse the cache and all its contents.
/// </summary>
public class IndexSegmentCache
{
    private CacheRoot _root;
    internal readonly int EntryCapacity;

    /// <summary>
    /// Create a new index segment cache.
    /// </summary>
    public IndexSegmentCache()
    {
        _root = new CacheRoot(ImmutableDictionary<CacheKey, CacheValue>.Empty);
        EntryCapacity = 1_000_000;
    }
    
    /// <summary>
    /// Get the index segment for the given entity id, if it is not in the cache, cache it, then update the cache at the
    /// given location so that it contains the new segment.
    /// </summary>
    public EntitySegment Get(EntityId entityId, IDb db)
    {
        var key = CacheKey.Create(IndexType.EAVTCurrent, entityId);
        if (_root.TryGetValue(key, out var segment))
            return (EntitySegment)segment;

        segment = db.Snapshot.GetEntitySegment(db, entityId);
        UpdateEntry(key, segment);
        return (EntitySegment)segment;
    }

    /// <summary>
    /// Adds the given index segment to the cache, under the given entity id.
    /// </summary>
    public void Add(EntityId id, IndexSegment segment)
    {
        var key = CacheKey.Create(IndexType.EAVTCurrent, id);
        if (_root.TryGetValue(key, out _))
            return;
        
        UpdateEntry(key, segment);
    }
    
    
    public void AddReverse(EntityId eid, AttributeId aid, IndexSegment segment)
    {
        var key = CacheKey.Create(IndexType.VAETCurrent, aid, eid);
        if (_root.TryGetValue(key, out _))
            return;
        UpdateEntry(key, segment);
    }

    /// <summary>
    /// Get a segment for all the datoms that point to the given entity id via their value for the given attribute.
    /// </summary>
    public EntityIds GetReverse(AttributeId attributeId, EntityId entityId, IDb db)
    {
        var key = CacheKey.Create(IndexType.VAETCurrent, attributeId, entityId);
        if (_root.TryGetValue(key, out var segment))
            return (EntityIds)segment;
        
        segment = db.Snapshot.GetEntityIdsPointingTo(attributeId, entityId);
        UpdateEntry(key, segment);
        return (EntityIds)segment;
    }
    
    /// <summary>
    /// Get a segment for all the datoms that point to the given entity id.
    /// </summary>
    public IndexSegment GetReferences(EntityId entityId, IDb db)
    {
        var key = CacheKey.Create(IndexType.VAETCurrent, entityId);
        if (_root.TryGetValue(key, out var segment))
            return (IndexSegment)segment;
        
        segment = db.Snapshot.Datoms(SliceDescriptor.CreateReferenceTo(entityId));
        UpdateEntry(key, segment);
        return (IndexSegment)segment;
    }
    
    /// <summary>
    /// Creates a copy of the cache with the given datoms evicted, and the new datoms added.
    /// </summary>
    public IndexSegmentCache ForkAndEvict(StoreResult result, AttributeCache attributeCache, out IndexSegment newDatoms)
    {
        var newRoot = _root.EvictNew(result, out newDatoms);
        return new IndexSegmentCache {  _root = newRoot };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateEntry(CacheKey key, object segment)
    {
        while (true)
        {
            var oldRoot = _root;
            var newRoot = oldRoot.With(key, segment, this);
            var result = Interlocked.CompareExchange(ref _root, newRoot, oldRoot);
            
            if (ReferenceEquals(result, oldRoot))
                return;
        }
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        _root = new CacheRoot(ImmutableDictionary<CacheKey, CacheValue>.Empty);
    }

}
