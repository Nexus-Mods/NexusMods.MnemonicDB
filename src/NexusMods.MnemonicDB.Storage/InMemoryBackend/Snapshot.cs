using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Storage.InMemoryBackend;

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
