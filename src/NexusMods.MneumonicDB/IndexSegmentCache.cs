using System;
using System.Collections.Concurrent;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;

namespace NexusMods.MneumonicDB;

/// <summary>
/// A cache for index segments, given a key type and a iterator factory, caches the results
/// of the factory in a segment for fast access.
/// </summary>
internal readonly struct IndexSegmentCache<TKey>(Func<IDb, TKey, IIterator> iteratorFactory)
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, IndexSegment> _cache = new();

    public IndexSegment Get(IDb snapshot, TKey key)
    {
        if (_cache.TryGetValue(key, out var segment))
            return segment;

        var iterator = iteratorFactory(snapshot, key);
        return Add(key, iterator);
    }

    private IndexSegment Add<TIterator>(TKey key, TIterator segment)
    where TIterator : IIterator
    {

        var builder = new IndexSegmentBuilder(128);
        while (segment.Valid)
        {
            builder.Add(segment.Current);
            segment.Next();
        }

        var built = builder.Build();
        _cache.TryAdd(key, built);
        return built;

    }
}
