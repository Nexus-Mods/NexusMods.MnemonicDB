using System;
using System.Collections.Generic;
using System.Threading;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB.Caching;


/// <summary>
/// A cache of index segments, implemented as an immutable LRU cache, immutable so that each subsequent
/// Db instance can reuse the cache and all its contents.
/// </summary>
public class IndexSegmentCache<TKey, TValue>
    where TKey : notnull
{
    private CacheRoot<TKey> _root;
    private readonly CacheStrategy<TKey,TValue> _strategy;

    /// <summary>
    /// Create a new index segment cache.
    /// </summary>
    public IndexSegmentCache(CacheStrategy<TKey, TValue> strategy)
    {
        _root = CacheRoot<TKey>.Create();
        _strategy = strategy;
    }

    private IndexSegmentCache(CacheStrategy<TKey, TValue> strategy, CacheRoot<TKey> newRoot)
    {
        _strategy = strategy;
        _root = newRoot;
    }


    /// <summary>
    /// Forks the cache and evicts entries based on the given store result's recently added datoms.
    /// </summary>
    public IndexSegmentCache<TKey, TValue> Fork(IReadOnlyList<IDatomLikeRO> segment, CacheStrategy<TKey,TValue> newStrategy)
    {
        var newRoot = _root.Evict(_strategy.GetKeysFromRecentlyAdded(segment));
        return new IndexSegmentCache<TKey, TValue>(newStrategy, newRoot);
    }

    /// <summary>
    /// Get a value (possibly cached) for the given key. CacheHit is set to true if the value was already
    /// in the cache.
    /// </summary>
    public TValue GetValue(TKey key, IDb context, out bool cacheHit)
    {
        if (_root.Cache.TryGetValue(key, out var value))
        {
            value.Hit();
            cacheHit = true;
            return _strategy.GetValue(key, context, value.Segment);
        }

        var valueBytes = _strategy.GetBytes(key);
        AddValue(key, valueBytes);
        cacheHit = false;
        return _strategy.GetValue(key, context, AddValue(key, valueBytes));
    }

    /// <summary>
    /// Adds the given value to the cache, and returns the actual stored value (may differ from the input if
    /// there was contention on the cache).
    /// </summary>
    public Memory<byte> AddValue(TKey key, Memory<byte> valueBytes)
    {
        while (true)
        {
            var oldRoot = _root;
            
            // If the cache somehow now contains the key, return the value.
            if (oldRoot.Cache.TryGetValue(key, out var newlyAdded))
                return newlyAdded.Segment;
            
            // Otherwise, add the new value to the cache.
            var newRoot = oldRoot.With(key, valueBytes).Evict(_strategy.MaxBytes, _strategy.MaxEntries, _strategy.EvictPercentage);
            var result = Interlocked.CompareExchange(ref _root, newRoot, oldRoot);
            if (ReferenceEquals(result, oldRoot))
                return valueBytes;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{typeof(TValue).Name} Cache with {_root.Cache.Count} ({_root.Size}) entries";
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        _root = CacheRoot<TKey>.Create();
    }
}
