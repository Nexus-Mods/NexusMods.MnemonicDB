using System;
using System.Collections.Immutable;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Storage.InMemoryBackend;

public class SortedSetIterator : IDatomSource, IIterator
{
    private readonly AttributeRegistry _registry;
    private int _offset;

    public SortedSetIterator(ImmutableSortedSet<byte[]> set, AttributeRegistry registry)
    {
        _registry = registry;
        Set = set;
        _offset = -1;
    }


    public ImmutableSortedSet<byte[]> Set { get; set; }

    public void Dispose()
    {
        Set = ImmutableSortedSet<byte[]>.Empty;
        _offset = -1;
    }

    public IIterator SeekLast()
    {
        _offset = Set.Count - 1;
        return this;
    }

    IIterator ISeekableIterator.Seek(ReadOnlySpan<byte> datom)
    {
        var result = Set.IndexOf(datom.ToArray());
        if (result < 0)
            _offset = ~result;
        else
            _offset = result;
        return this;
    }

    public IIterator SeekStart()
    {
        _offset = 0;
        return this;
    }

    public bool Valid => _offset >= 0 && _offset < Set.Count;

    public void Next()
    {
        _offset++;
    }

    public void Prev()
    {
        _offset--;
    }

    public ReadOnlySpan<byte> Current => Set[_offset];
    public IAttributeRegistry Registry => _registry;
}
