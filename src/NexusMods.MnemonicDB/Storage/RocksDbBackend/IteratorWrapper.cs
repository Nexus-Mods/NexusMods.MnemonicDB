using System;
using NexusMods.MnemonicDB.Abstractions;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal readonly struct RocksDbIteratorWrapper(Iterator iterator) : ILowLevelIterator
{
    public void SeekTo(ReadOnlySpan<byte> span) => 
        iterator.Seek(span);

    public void SeekToPrev(ReadOnlySpan<byte> span) => 
        iterator.SeekForPrev(span);

    public void Next() => 
        iterator.Next();

    public void Prev() => 
        iterator.Prev();

    public bool IsValid => 
        iterator.Valid();
    
    public Ptr Key
    {
        get
        {
            unsafe
            {
                var kPtr = Native.Instance.rocksdb_iter_key(iterator.Handle, out var kLen);
                return new Ptr((byte*)kPtr, (int)kLen);
            }
        }
    }
    public Ptr Value
    {
        get
        {
            unsafe
            {
                var kPtr = Native.Instance.rocksdb_iter_value(iterator.Handle, out var kLen);
                return new Ptr((byte*)kPtr, (int)kLen);
            }
        }
    }
    public void Dispose() => 
        iterator.Dispose();
}
