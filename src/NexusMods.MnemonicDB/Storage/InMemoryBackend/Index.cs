using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Storage.Abstractions;

namespace NexusMods.MnemonicDB.Storage.InMemoryBackend;

/// <summary>
/// An in-memory index.
/// </summary>
public class Index<TDatomComparator>(IndexStore store) :
    AIndex<TDatomComparator, IndexStore>(store), IInMemoryIndex, IComparer<byte[]>
    where TDatomComparator : IDatomComparator
{
    /// <inheritdoc />
    public int Compare(byte[]? x, byte[]? y)
    {
        unsafe
        {
            fixed (byte* xPtr = x)
            fixed (byte* yPtr = y)
                return TDatomComparator.Compare(xPtr, x!.Length, yPtr, y!.Length);
        }
    }

    public ImmutableSortedSet<byte[]> Set => store.Set;
}
