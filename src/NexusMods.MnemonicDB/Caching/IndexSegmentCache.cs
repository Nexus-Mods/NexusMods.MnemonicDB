using System.Threading;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Caching;


/// <summary>
/// A cache of index segments, implemented as an immutable LRU cache, immutable so that each subsequent
/// Db instance can reuse the cache and all its contents.
/// </summary>
public class IndexSegmentCache<TKey>
    where TKey : notnull
{
    private CacheRoot<TKey> _root;
    private readonly CacheStrategy<TKey> _strategy;

    /// <summary>
    /// Create a new index segment cache.
    /// </summary>
    public IndexSegmentCache(CacheStrategy<TKey> strategy)
    {
        _root = CacheRoot<TKey>.Create();
        _strategy = strategy;
    }

    private IndexSegmentCache(CacheStrategy<TKey> strategy, CacheRoot<TKey> newRoot)
    {
        _strategy = strategy;
        _root = newRoot;
    }


    /// <summary>
    /// Forks the cache and evicts entries based on the given store result's recently added datoms.
    /// </summary>
    public IndexSegmentCache<TKey> Fork(Datoms segment, CacheStrategy<TKey> newStrategy)
    {
        var newRoot = _root.Evict(_strategy.GetKeysFromRecentlyAdded(segment));
        return new IndexSegmentCache<TKey>(newStrategy, newRoot);
    }

    /// <summary>
    /// Get a value (possibly cached) for the given key. CacheHit is set to true if the value was already
    /// in the cache.
    /// </summary>
    public Datoms GetDatoms(TKey key, IDb context, out bool cacheHit)
    {
        if (_root.Cache.TryGetValue(key, out var value))
        {
            value.Hit();
            cacheHit = true;
            return _strategy.GetDatoms(key);
        }

        var datoms = _strategy.GetDatoms(key);
        AddValue(key, datoms);
        cacheHit = false;
        return datoms;
    }

    /// <summary>
    /// Adds the given value to the cache, and returns the actual stored value (may differ from the input if
    /// there was contention on the cache).
    /// </summary>
    public Datoms AddValue(TKey key, Datoms datoms)
    {
        while (true)
        {
            var oldRoot = _root;
            
            // If the cache somehow now contains the key, return the value.
            if (oldRoot.Cache.TryGetValue(key, out var newlyAdded))
                return newlyAdded.Segment;
            
            // Otherwise, add the new value to the cache.
            var newRoot = oldRoot.With(key, datoms).Evict(_strategy.MaxBytes, _strategy.MaxEntries, _strategy.EvictPercentage);
            var result = Interlocked.CompareExchange(ref _root, newRoot, oldRoot);
            if (ReferenceEquals(result, oldRoot))
                return datoms;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{_strategy.GetType().Name} Cache with {_root.Cache.Count} ({_root.Size}) entries";
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        _root = CacheRoot<TKey>.Create();
    }
}
