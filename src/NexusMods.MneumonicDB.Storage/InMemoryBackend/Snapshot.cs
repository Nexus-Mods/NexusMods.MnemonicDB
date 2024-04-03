using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomComparators;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;

namespace NexusMods.MneumonicDB.Storage.InMemoryBackend;

public class Snapshot : ISnapshot
{
    private readonly ImmutableSortedSet<byte[]>[] _indexes;
    private readonly AttributeRegistry _registry;

    public Snapshot(ImmutableSortedSet<byte[]>[] indexes, AttributeRegistry registry)
    {
        _registry = registry;
        _indexes = indexes;
    }

    public void Dispose() { }

    public IDatomSource GetIterator(IndexType type, bool historical)
    {
        if (!historical)
            return GetIteratorInner(type);

        switch (type)
        {
            case IndexType.EAVTCurrent:
            case IndexType.EAVTHistory:
                return new TemporalSetIterator<EAVTComparator<AttributeRegistry>>(GetIteratorInner(IndexType.EAVTCurrent), GetIteratorInner(IndexType.EAVTHistory), _registry);
            case IndexType.AEVTCurrent:
            case IndexType.AEVTHistory:
                return new TemporalSetIterator<AEVTComparator<AttributeRegistry>>(GetIteratorInner(IndexType.AEVTCurrent), GetIteratorInner(IndexType.AEVTHistory), _registry);
            case IndexType.AVETCurrent:
            case IndexType.AVETHistory:
                return new TemporalSetIterator<AVETComparator<AttributeRegistry>>(GetIteratorInner(IndexType.AVETCurrent), GetIteratorInner(IndexType.AVETHistory), _registry);
            case IndexType.VAETCurrent:
            case IndexType.VAETHistory:
                return new TemporalSetIterator<VAETComparator<AttributeRegistry>>(GetIteratorInner(IndexType.VAETCurrent), GetIteratorInner(IndexType.VAETHistory), _registry);
            case IndexType.TxLog:
                return GetIteratorInner(IndexType.TxLog);
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown index type");
        }
    }

    private SortedSetIterator GetIteratorInner(IndexType type)
    {
        return new SortedSetIterator(_indexes[(int)type], _registry);
    }

    public IEnumerable<Datom> Datoms(IndexType type, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var idxLower = _indexes[(int)type].IndexOf(a.ToArray());
        var idxUpper = _indexes[(int)type].IndexOf(b.ToArray());

        if (idxLower < 0)
            idxLower = ~idxLower;

        if (idxUpper < 0)
            idxUpper = ~idxUpper;

        var lower = idxLower;
        var upper = idxUpper;
        var reverse = false;

        if (idxLower > idxUpper)
        {
            lower = idxUpper;
            upper = idxLower;
            reverse = true;
        }

        return DatomsInner(type, reverse, lower, upper);

    }

    private IEnumerable<Datom> DatomsInner(IndexType type, bool reverse, int lower, int upper)
    {
        if (!reverse)
        {
            for (var i = lower; i < upper; i++)
            {
                yield return new Datom(_indexes[(int)type].ElementAt(i), _registry);
            }
        }
        else
        {
            for (var i = upper; i > lower; i--)
            {
                yield return new Datom(_indexes[(int)type].ElementAt(i), _registry);
            }
        }
    }
}
