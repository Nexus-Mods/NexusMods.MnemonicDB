using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Caching;

/// <summary>
/// A root node for a cache, containing the size of the cache and the cache itself. Immutable as this data gets shared
/// between multiple iterations of a database.
/// </summary>
public record CacheRoot(ulong TotalCount, ImmutableDictionary<object, CacheValue> Cache)
{
    /// <summary>
    /// Create a new empty cache root.
    /// </summary>
    /// <returns></returns>
    public static CacheRoot Create()
    {
        return new CacheRoot(0, ImmutableDictionary<object, CacheValue>.Empty);
    }

    /// <summary>
    /// Create a new instance of the cache root with the given key and data added to the cache.
    /// </summary>
    public CacheRoot With(object key, Datoms data)
    {
        return new CacheRoot(TotalCount + (ulong)data.Count, Cache.SetItem(key, new CacheValue(data)));
    }
    
    /// <summary>
    /// Evicts entries from the cache to make room for new entries, only performs work if the cache is over the given size,
    /// and at which point it will evict the given percentage of the cache (oldest entries first).
    /// </summary>
    public CacheRoot Evict(ulong maxTotalCount, int maxEntries, double evictPercentage = 0.5)
    {
        if (TotalCount <= maxTotalCount && Cache.Count <= maxEntries)
            return this;
        
        var entriesByAge = Cache
            .OrderByDescending(e => e.Value.LastAccessed)
            .ToList();
        
        var desiredTotalCount = maxTotalCount * evictPercentage;
        var desiredEntries = (int) (maxEntries * evictPercentage);
        var currentSize = TotalCount; 
        var currentEntries = Cache.Count;
        var stopPoint = 0;
        
        while (currentSize > desiredTotalCount || currentEntries > desiredEntries)
        {
            var entry = entriesByAge[stopPoint];
            currentSize -= (ulong)entry.Value.Segment.Count;
            currentEntries -= 1;
            stopPoint += 1;
        }
        
        var builder = Cache.ToBuilder();
        for (var i = 0; i < stopPoint; i++)
        {
            builder.Remove(entriesByAge[i].Key);
        }
        return new CacheRoot(currentSize, builder.ToImmutable());
    }

    /// <summary>
    /// Evicts entries from the cache based on the given keys.
    /// </summary>
    public CacheRoot Evict(Datoms added) 
    {
        var builder = Cache.ToBuilder();
        var currentSize = TotalCount;
        foreach (var datom in added)
        {
            // EAVT
            currentSize -= builder.Remove((typeof(EntityIdSlice), datom.E)) ? 1UL : 0;
        
            // Backrefs
            if (datom.Tag == ValueTag.Reference)
                currentSize -= builder.Remove((typeof(BackRefSlice), datom.A, (EntityId)datom.Value)) ? 1UL : 0;
        }
        return new CacheRoot(currentSize, builder.ToImmutable());
    }
}
