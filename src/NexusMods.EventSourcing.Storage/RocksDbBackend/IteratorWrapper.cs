using System;
using NexusMods.EventSourcing.Abstractions.DatomIterators;
using NexusMods.EventSourcing.Abstractions.Internals;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.RocksDbBackend;

internal class IteratorWrapper : IDatomSource, IIterator
{
    private readonly AttributeRegistry _registry;
    private Iterator _iterator;

    public IteratorWrapper(Iterator iterator, AttributeRegistry registry)
    {
        _registry = registry;
        _iterator = iterator;
    }

    public void Dispose()
    {
        _iterator.Dispose();
        _iterator = null!;
    }

    public IIterator SeekLast()
    {
        _iterator.SeekToLast();
        return this;
    }

    public IIterator Seek(ReadOnlySpan<byte> datom)
    {
        _iterator.Seek(datom);
        return this;
    }

    public IIterator SeekStart()
    {
        _iterator.SeekToFirst();
        return this;
    }

    public bool Valid => _iterator.Valid();

    public void Next()
    {
        _iterator.Next();
    }

    public void Prev()
    {
        _iterator.Prev();
    }


    public ReadOnlySpan<byte> Current => _iterator.GetKeySpan();
    public IAttributeRegistry Registry => _registry;
}
