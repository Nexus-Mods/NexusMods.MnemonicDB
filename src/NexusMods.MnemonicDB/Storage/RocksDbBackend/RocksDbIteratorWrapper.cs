using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal struct RocksDbIteratorWrapper : IRefDatomPeekingEnumerator, ILowLevelIterator
{
    private Ptr _key;
    private bool _started;
    private readonly Iterator _iterator;

    public RocksDbIteratorWrapper(Iterator iterator)
    {
        _iterator = iterator;
        _started = false;
    }

    public void Dispose() => 
        _iterator.Dispose();

    public unsafe bool MoveNext<TSliceDescriptor>(TSliceDescriptor descriptor, bool useHistory = false) 
        where TSliceDescriptor : ISliceDescriptor, allows ref struct
    {
        if (_started == false)
        {
            descriptor.Reset(this, useHistory);
            _started = true;
        }
        else
        {
            descriptor.MoveNext(this);
        }
            
        if (_iterator.Valid())
        {
            var kPtr = Native.Instance.rocksdb_iter_key(_iterator.Handle, out var kLen);
            _key = new Ptr((byte*)kPtr, (int)kLen);
            return descriptor.ShouldContinue(_key.Span, useHistory);
        }
        return false;
    }

    public unsafe bool TryGetRetractionId(out TxId id)
    {
        _iterator.Next();
        if (!_iterator.Valid())
        {
            _iterator.Prev();
            id = default;
            return false;
        }
        var keyPtr = Native.Instance.rocksdb_iter_key(_iterator.Handle, out var keyLen);
        var prefix = KeyPrefix.Read(new ReadOnlySpan<byte>((byte*)keyPtr, (int)keyLen));
        _iterator.Prev();
        
        if (!prefix.IsRetract)
        {
            id = default;
            return false;
        }
        
        // The way indexes are sorted, there's never a case where a retraction comes after an assert that it is not retraction for
        id = prefix.T;
        return true;
    }

    public KeyPrefix KeyPrefix => _key.Read<KeyPrefix>(0);
    public Ptr Current => _key;
    public Ptr ValueSpan => _key.SliceFast(KeyPrefix.Size);

    public unsafe Ptr ExtraValueSpan
    {
        get 
        {
            var vPtr = Native.Instance.rocksdb_iter_value(_iterator.Handle, out var vLen);
            return new Ptr((byte*)vPtr, (int)vLen);
        }
    }
    
    
    public void SeekTo(ReadOnlySpan<byte> span) => 
        _iterator.Seek(span);

    public void SeekToPrev(ReadOnlySpan<byte> span) => 
        _iterator.SeekForPrev(span);

    public void Next() => 
        _iterator.Next();

    public void Prev() => 
        _iterator.Prev();
}
