using System;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Caching;

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
    public readonly DatomList Segment;
    
    /// <summary>
    /// Create a new cache value.
    /// </summary>
    /// <param name="segment"></param>
    public CacheValue(DatomList segment)
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
