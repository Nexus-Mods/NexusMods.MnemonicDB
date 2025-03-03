using System;
using NexusMods.MnemonicDB.Abstractions;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal readonly struct IteratorWrapper(Iterator iterator) : ILowLevelIterator
{
    public void SeekTo(ReadOnlySpan<byte> span)
    {
        iterator.Seek(span);
    }

    public void SeekToPrev(ReadOnlySpan<byte> span)
    {
        iterator.SeekForPrev(span);
    }

    public void Next()
    {
        iterator.Next();
    }

    public void Prev()
    {
        iterator.Prev();
    }
}
