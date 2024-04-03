using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Storage.Abstractions;

namespace NexusMods.MneumonicDB.Storage.InMemoryBackend;

public class Index<TDatomComparator>(AttributeRegistry registry, IndexStore store) :
    AIndex<TDatomComparator, IndexStore>(registry, store), IInMemoryIndex, IComparer<byte[]>
    where TDatomComparator : IDatomComparator<AttributeRegistry>
{
    public int Compare(byte[]? x, byte[]? y)
    {
        return Compare(x.AsSpan(), y.AsSpan());
    }

    public ImmutableSortedSet<byte[]> Set => store.Set;
}
