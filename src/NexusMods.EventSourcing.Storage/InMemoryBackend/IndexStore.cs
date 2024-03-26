using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.InMemoryBackend;

public class IndexStore : IIndexStore
{
    public IndexType Type { get; }
    public ImmutableSortedSet<byte[]> Set { get; private set; }

    public IndexStore(IndexType type)
    {
        Type = type;
        Set = ImmutableSortedSet<byte[]>.Empty;
    }

    public void Init(IComparer<byte[]> sorter)
    {
        Set = ImmutableSortedSet<byte[]>.Empty.WithComparer(sorter);
    }

    public IDatomIterator GetIterator()
    {
        return new SortedSetIterator(Set);
    }


    public void Commit(List<(bool IsDelete, byte[] Data)> datoms)
    {
        var builder = Set.ToBuilder();
        foreach (var (isRetract, datom) in datoms)
        {
            if (isRetract)
                builder.Remove(datom);
            else
                builder.Add(datom);
        }
        Set = builder.ToImmutable();
    }
}
