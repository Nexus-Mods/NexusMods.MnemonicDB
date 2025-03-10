using System;
using System.Threading;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Caching;


/// <summary>
/// A cache of index segments, implemented as an immutable LRU cache, immutable so that each subsequent
/// Db instance can reuse the cache and all its contents.
/// </summary>
public class IndexSegmentCache<TKey, TValue>
    where TKey : notnull
    where TValue : IEquatable<TValue>
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
    public IndexSegmentCache<TKey, TValue> Fork(IndexSegment segment)
    {
        var newRoot = _root.Evict(_strategy.GetKeysFromRecentlyAdded(segment));
        return new IndexSegmentCache<TKey, TValue>(_strategy, newRoot);
    }

    /// <summary>
    /// Get a value (possibly cached) for the given key.
    /// </summary>
    public TValue GetValue(TKey key, IDb context)
    {
        if (_root.Cache.TryGetValue(key, out var value))
        {
            value.Hit();
            return _strategy.GetValue(key, context, value.Segment);
        }

        var valueBytes = _strategy.GetBytes(key, context);
        
        while (true)
        {
            var oldRoot = _root;
            
            // If the cache somehow now contains the key, return the value.
            if (oldRoot.Cache.TryGetValue(key, out var newlyAdded))
                return _strategy.GetValue(key, context, newlyAdded.Segment);
            
            // Otherwise, add the new value to the cache.
            var newRoot = oldRoot.With(key, valueBytes).Evict(_strategy.MaxBytes, _strategy.MaxEntries, _strategy.EvictPercentage);
            var result = Interlocked.CompareExchange(ref _root, newRoot, oldRoot);
            if (ReferenceEquals(result, oldRoot))
                return _strategy.GetValue(key, context, valueBytes);
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{typeof(TValue).Name} Cache with {_root.Cache.Count} ({_root.Size} entries";
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        _root = CacheRoot<TKey>.Create();
    }

}
