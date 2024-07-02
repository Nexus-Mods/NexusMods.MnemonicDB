using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB;

/// <summary>
/// A cache for index segments, given a key type and a iterator factory, caches the results
/// of the factory in a segment for fast access.
/// </summary>
internal readonly struct IndexSegmentCache<TKey>(Func<IDb, TKey, IndexSegment> segmentFactory, IAttributeRegistry registry)
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, IndexSegment> _cache = new();

    public IndexSegment Get(IDb snapshot, TKey key)
    {
        if (_cache.TryGetValue(key, out var segment))
            return segment;

        var newSegment = segmentFactory(snapshot, key);
        _cache.TryAdd(key, newSegment);
        return newSegment;
    }
}
