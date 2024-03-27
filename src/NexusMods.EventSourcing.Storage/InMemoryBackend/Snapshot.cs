using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.DatomIterators;

namespace NexusMods.EventSourcing.Storage.InMemoryBackend;

public class Snapshot : ISnapshot
{
    private readonly ImmutableSortedSet<byte[]>[] _indexes;
    private readonly AttributeRegistry _registry;

    public Snapshot(ImmutableSortedSet<byte[]>[] indexes, AttributeRegistry registry)
    {
        _registry = registry;
        _indexes = indexes;
    }

    public void Dispose()
    {

    }

    public IDatomSource GetIterator(IndexType type)
    {
        return new SortedSetIterator(_indexes[(int)type], _registry);
    }
}
