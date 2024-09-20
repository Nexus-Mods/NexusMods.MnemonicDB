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

[StructLayout(LayoutKind.Explicit, Size = 12)]
public readonly struct CacheKey : IEquatable<CacheKey>
{
    [FieldOffset(0)]
    public readonly IndexType IndexType;
    
    [FieldOffset(1)]
    public readonly AttributeId AttributeId;
    
    [FieldOffset(4)]
    public readonly EntityId EntityId;
    
    public CacheKey(IndexType indexType, AttributeId attributeId, EntityId entityId)
    {
        IndexType = indexType;
        AttributeId = attributeId;
        EntityId = entityId;
    }
    
    public static CacheKey Create(IndexType indexType, AttributeId attributeId, EntityId entityId) => new(indexType, attributeId, entityId);
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

public struct CacheValue : IEquatable<CacheValue>
{
    public long LastAccessed;
    public readonly IndexSegment Segment;
    
    public CacheValue(long lastAccessed, IndexSegment segment)
    {
        LastAccessed = lastAccessed;
        Segment = segment;
    }

    /// <summary>
    /// Update the last accessed time to now.
    /// </summary>
    public void Hit()
    {
        LastAccessed = DateTime.UtcNow.Ticks;
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
    public bool TryGetValue(CacheKey key, out IndexSegment segment)
    {
        if (_entries.TryGetValue(key, out var value))
        {
            value.Hit();
            segment = value.Segment;
            return true;
        }
        segment = default;
        return false;
    }

    /// <summary>
    /// Create a new cache root with the given segment added.
    /// </summary>
    public CacheRoot With(CacheKey key, IndexSegment segment, IndexSegmentCache cache)
    {
        var newEntries = _entries.SetItem(key, new CacheValue(DateTime.UtcNow.Ticks, segment));
        if (newEntries.Count > cache._entryCapacity)
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
        var droppedSize = Size.Zero;
        foreach (var kv in toDrop)
        {
            builder.Remove(kv.Key);
            droppedSize += kv.Value.Segment.DataSize;
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
            
            if (datom.Prefix.ValueTag != ValueTags.Reference)
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
    internal readonly int _entryCapacity;
    internal readonly Size _maxSize;

    /// <summary>
    /// Create a new index segment cache.
    /// </summary>
    public IndexSegmentCache()
    {
        _root = new CacheRoot(ImmutableDictionary<CacheKey, CacheValue>.Empty);
        _entryCapacity = 1_000_000;
    }
    
    /// <summary>
    /// Get the index segment for the given entity id, if it is not in the cache, cache it, then update the cache at the
    /// given location so that it contains the new segment.
    /// </summary>
    public IndexSegment Get(EntityId entityId, IDb db)
    {
        var key = CacheKey.Create(IndexType.EAVTCurrent, entityId);
        if (_root.TryGetValue(key, out var segment))
            return segment;

        segment = db.Snapshot.Datoms(SliceDescriptor.Create(entityId));
        UpdateEntry(key, segment);
        return segment;
    }

    /// <summary>
    /// Get a segment for all the datoms that point to the given entity id via their value for the given attribute.
    /// </summary>
    public IndexSegment GetReverse(AttributeId attributeId, EntityId entityId, IDb db)
    {
        var key = CacheKey.Create(IndexType.VAETCurrent, attributeId, entityId);
        if (_root.TryGetValue(key, out var segment))
            return segment;
        
        segment = db.Snapshot.Datoms(SliceDescriptor.Create(attributeId, entityId));
        UpdateEntry(key, segment);
        return segment;
    }
    
    /// <summary>
    /// Get a segment for all the datoms that point to the given entity id.
    /// </summary>
    public IndexSegment GetReferences(EntityId entityId, IDb db)
    {
        var key = CacheKey.Create(IndexType.VAETCurrent, entityId);
        if (_root.TryGetValue(key, out var segment))
            return segment;
        
        segment = db.Snapshot.Datoms(SliceDescriptor.CreateReferenceTo(entityId));
        UpdateEntry(key, segment);
        return segment;
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
    private void UpdateEntry(CacheKey key, IndexSegment segment)
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
    
}
