using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Storage.Abstractions;

namespace NexusMods.MnemonicDB.Storage.InMemoryBackend;

public class Index<TDatomComparator>(IndexStore store) :
    AIndex<TDatomComparator, IndexStore>(store), IInMemoryIndex, IComparer<byte[]>
    where TDatomComparator : IDatomComparator
{
    public int Compare(byte[]? x, byte[]? y)
    {
        return Compare(x.AsSpan(), y.AsSpan());
    }

    public ImmutableSortedSet<byte[]> Set => store.Set;
}
