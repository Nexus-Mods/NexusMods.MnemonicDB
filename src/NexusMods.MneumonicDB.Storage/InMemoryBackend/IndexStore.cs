﻿using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Storage.Abstractions;

namespace NexusMods.MneumonicDB.Storage.InMemoryBackend;

public class IndexStore : IIndexStore
{
    private readonly AttributeRegistry _registry;

    public IndexStore(IndexType type, AttributeRegistry registry)
    {
        _registry = registry;
        Type = type;
        Set = ImmutableSortedSet<byte[]>.Empty;
    }

    public ImmutableSortedSet<byte[]> Set { get; private set; }
    public IndexType Type { get; }

    public IDatomSource GetIterator()
    {
        return new SortedSetIterator(Set, _registry);
    }

    public void Init(IComparer<byte[]> sorter)
    {
        Set = ImmutableSortedSet<byte[]>.Empty.WithComparer(sorter);
    }


    public void Commit(List<(bool IsDelete, byte[] Data)> datoms)
    {
        var builder = Set.ToBuilder();
        foreach (var (isRetract, datom) in datoms)
            if (isRetract)
                builder.Remove(datom);
            else
                builder.Add(datom);
        Set = builder.ToImmutable();
    }
}
