using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Caching;

/// <summary>
/// A root node for a cache, containing the size of the cache and the cache itself. Immutable as this data gets shared
/// between multiple iterations of a database.
/// </summary>
public record CacheRoot<TKey>(Size Size, ImmutableDictionary<TKey, CacheValue> Cache)
    where TKey : notnull
{
    /// <summary>
    /// Create a new empty cache root.
    /// </summary>
    /// <returns></returns>
    public static CacheRoot<TKey> Create()
    {
        return new CacheRoot<TKey>(Size.Zero, ImmutableDictionary<TKey, CacheValue>.Empty);
    }

    /// <summary>
    /// Create a new instance of the cache root with the given key and data added to the cache.
    /// </summary>
    public CacheRoot<TKey> With(TKey key, Memory<byte> data)
    {
        return new CacheRoot<TKey>(Size + Size.FromLong(data.Length), Cache.SetItem(key, new CacheValue(data)));
    }
    
    /// <summary>
    /// Evicts entries from the cache to make room for new entries, only performs work if the cache is over the given size,
    /// and at which point it will evict the given percentage of the cache (oldest entries first).
    /// </summary>
    public CacheRoot<TKey> Evict(Size maxSize, int maxEntries, double evictPercentage = 0.5)
    {
        if (Size <= maxSize && Cache.Count <= maxEntries)
            return this;
        
        var entriesByAge = Cache
            .OrderByDescending(e => e.Value.LastAccessed)
            .ToList();
        
        var desiredSize = maxSize * evictPercentage;
        var desiredEntries = (int) (maxEntries * evictPercentage);
        var currentSize = Size; 
        var currentEntries = Cache.Count;
        var stopPoint = 0;
        
        while (currentSize > desiredSize || currentEntries > desiredEntries)
        {
            var entry = entriesByAge[stopPoint];
            currentSize -= Size.FromLong(entry.Value.Segment.Length);
            currentEntries -= 1;
            stopPoint += 1;
        }
        
        var builder = Cache.ToBuilder();
        for (var i = 0; i < stopPoint; i++)
        {
            builder.Remove(entriesByAge[i].Key);
        }
        return new CacheRoot<TKey>(currentSize, builder.ToImmutable());
    }

    /// <summary>
    /// Evicts entries from the cache based on the given keys.
    /// </summary>
    public CacheRoot<TKey> Evict(IEnumerable<TKey> getKeysFromRecentlyAdded) 
    {
        var builder = Cache.ToBuilder();
        var currentSize = Size;
        foreach (var key in getKeysFromRecentlyAdded)
        {
            if (!builder.TryGetValue(key, out var value)) 
                continue;
            
            currentSize -= Size.FromLong(value.Segment.Length);
            builder.Remove(key);
        }
        return new CacheRoot<TKey>(currentSize, builder.ToImmutable());
    }
}
