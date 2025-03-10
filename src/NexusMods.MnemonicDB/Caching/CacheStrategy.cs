using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Caching;

public abstract class CacheStrategy<TKey, TValue>
{
    /// <summary>
    /// The maximum number of entries in the cache.
    /// </summary>
    public int MaxEntries { get; set; }
    
    /// <summary>
    /// The maximum number of bytes in the cache.
    /// </summary>
    public Size MaxBytes { get; set; }
    
    /// <summary>
    /// When the caches are full we evict entries until we have this percentage of free space.
    /// </summary>
    public double EvictPercentage { get; set; }
    
    /// <summary>
    /// On a cache miss, this method will be called to get the actual bytes for the index segment
    /// </summary>
    public abstract Memory<byte> GetBytes(TKey key, IDb db);
    
    /// <summary>
    /// Construct a value from the given bytes, key, and db.
    /// </summary>
    public abstract TValue GetValue(TKey key, IDb db, Memory<byte> bytes);
    
    /// <summary>
    /// Get the keys to evict from the cache due to the given segment being recently added.
    /// </summary>
    public abstract IEnumerable<TKey> GetKeysFromRecentlyAdded(IndexSegment segment);
}
