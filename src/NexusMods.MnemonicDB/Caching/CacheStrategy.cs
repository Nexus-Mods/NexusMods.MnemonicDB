using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Caching;

/// <summary>
/// A cache strategy for a cache of index segments, defines the max size of the cache and how to get the bytes for a key.
/// </summary>
public class CacheStrategy
{
    /// <summary>
    /// The maximum number of entries in the cache.
    /// </summary>
    public int MaxEntries { get; set; } = 500_000;

    /// <summary>
    /// The maximum number of bytes in the cache.
    /// </summary>
    public ulong MaxTotalCount { get; set; } = 1024 * 1024 * 128;

    /// <summary>
    /// When the caches are full we evict entries until we have this percentage of free space.
    /// </summary>
    public double EvictPercentage { get; set; } = 0.25;
}
