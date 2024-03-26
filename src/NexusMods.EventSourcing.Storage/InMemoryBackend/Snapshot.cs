using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.InMemoryBackend;

public class Snapshot : ISnapshot
{
    private readonly ImmutableSortedSet<byte[]>[] _indexes;

    public Snapshot(ImmutableSortedSet<byte[]>[] indexes)
    {
        _indexes = indexes;
    }

    public void Dispose()
    {

    }

    public IDatomIterator GetIterator(IndexType type)
    {
        return new SortedSetIterator(_indexes[(int)type]);
    }
}
