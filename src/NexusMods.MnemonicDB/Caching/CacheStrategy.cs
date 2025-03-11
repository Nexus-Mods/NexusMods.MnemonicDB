using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Caching;

/// <summary>
/// A cache strategy for a cache of index segments, defines the max size of the cache and how to get the bytes for a key.
/// </summary>
public abstract class CacheStrategy<TKey, TValue>
{
    /// <summary>
    /// The maximum number of entries in the cache.
    /// </summary>
    public int MaxEntries { get; set; } = 100_000;

    /// <summary>
    /// The maximum number of bytes in the cache.
    /// </summary>
    public Size MaxBytes { get; set; } = Size.MB * 100;

    /// <summary>
    /// When the caches are full we evict entries until we have this percentage of free space.
    /// </summary>
    public double EvictPercentage { get; set; } = 0.25;
    
    /// <summary>
    /// On a cache miss, this method will be called to get the actual bytes for the index segment
    /// </summary>
    public abstract Memory<byte> GetBytes(TKey key);
    
    /// <summary>
    /// Construct a value from the given bytes, key, and db. Useful for wrapping the bytes into a segment
    /// </summary>
    public abstract TValue GetValue(TKey key, IDb db, Memory<byte> bytes);
    
    /// <summary>
    /// Get the keys to evict from the cache due to the given segment being recently added.
    /// </summary>
    public abstract IEnumerable<TKey> GetKeysFromRecentlyAdded(IndexSegment segment);
}
