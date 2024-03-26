using System;
using System.Collections.Immutable;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.InMemoryBackend;

public class SortedSetIterator : IDatomIterator
{
    public int Offset { get; set; }

    public SortedSetIterator(ImmutableSortedSet<byte[]> set)
    {
        Set = set;
        Offset = -1;
    }


    public ImmutableSortedSet<byte[]> Set { get; set; }

    public void Dispose()
    {
        Set = ImmutableSortedSet<byte[]>.Empty;
        Offset = -1;
    }

    public bool Valid => Offset >= 0 && Offset < Set.Count;
    public void Next()
    {
        Offset++;
    }

    public void Seek(ReadOnlySpan<byte> datom)
    {
        var result = Set.IndexOf(datom.ToArray());
        if (result < 0)
            Offset = ~result;
        else
            Offset = result;
    }

    public ReadOnlySpan<byte> Current => Set[Offset];
    public void SeekStart()
    {
        Offset = 0;
    }
}
