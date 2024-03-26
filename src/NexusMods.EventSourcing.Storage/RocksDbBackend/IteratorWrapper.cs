using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.RocksDbBackend;

internal class IteratorWrapper : IDatomIterator
{
    private readonly Iterator _iterator;

    public IteratorWrapper(Iterator iterator)
    {
        _iterator = iterator;
    }

    public void Dispose()
    {
        _iterator.Dispose();
    }

    public bool Valid => _iterator.Valid();
    public void Next()
    {
        _iterator.Next();
    }

    public void Seek(ReadOnlySpan<byte> datom)
    {
        _iterator.Seek(datom);
    }
    public ReadOnlySpan<byte> Current => _iterator.GetKeySpan();
}
