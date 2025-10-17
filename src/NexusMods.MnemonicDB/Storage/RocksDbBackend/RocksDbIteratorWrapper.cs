using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal struct RocksDbIteratorWrapper(Iterator iterator) : ILowLevelIterator, IRefDatomPeekingEnumerator
{
    private Ptr _key;
    private bool _started = false;

    public void Dispose() => 
        iterator.Dispose();

    public unsafe bool MoveNext<TSliceDescriptor>(TSliceDescriptor descriptor, bool useHistory = false) 
        where TSliceDescriptor : ISliceDescriptor, allows ref struct
    {
        if (!_started)
        {
            descriptor.Reset(this, useHistory);
            _started = true;
        }
        else
        {
            iterator.Next();
        }

        if (!iterator.Valid()) 
            return false;
        
        var kPtr = Native.Instance.rocksdb_iter_key(iterator.Handle, out var kLen);
        _key = new Ptr((byte*)kPtr, (int)kLen);
        return descriptor.ShouldContinue(_key.Span, useHistory);
    }


    public KeyPrefix Prefix => _key.Read<KeyPrefix>(0);
    public Ptr Current => _key;
    public Ptr ValueSpan => _key.SliceFast(KeyPrefix.Size);

    public unsafe Ptr ExtraValueSpan
    {
        get 
        {
            var vPtr = Native.Instance.rocksdb_iter_value(iterator.Handle, out var vLen);
            return new Ptr((byte*)vPtr, (int)vLen);
        }
    }

    public bool TryGetRetractionId(out TxId id)
    {
        throw new NotImplementedException();
    }


    public void SeekTo(ReadOnlySpan<byte> span) => 
        iterator.Seek(span);

    public void SeekToPrev(ReadOnlySpan<byte> span)
    {
        iterator.Seek(span);
        if (!iterator.Valid())
            iterator.SeekToLast();
        else
            iterator.Prev();
    }

    public void Next() => 
        iterator.Next();

    public void Prev() => 
        iterator.Prev();
}
