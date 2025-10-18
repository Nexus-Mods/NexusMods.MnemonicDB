using System;
using System.Diagnostics;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal struct RocksDbIteratorWrapper(RocksDbSharp.RocksDb db, RocksDbSharp.Snapshot snapshot, RocksDbSharp.ReadOptions globalReadOptions) 
    : ILowLevelIterator, IRefDatomEnumerator
{
    private Ptr _key;
    private Iterator? _iterator = null;
    private RocksDbSharp.ReadOptions? _readOptions = null;

    public void Dispose() => 
        _iterator?.Dispose();

    public unsafe bool MoveNext<TSliceDescriptor>(TSliceDescriptor descriptor, bool useHistory = false) 
        where TSliceDescriptor : ISliceDescriptor, allows ref struct
    {
        if (_iterator == null)
            Setup(descriptor, useHistory);
        else
            _iterator!.Next();

        if (!_iterator!.Valid()) 
            return false;
        
        var kPtr = Native.Instance.rocksdb_iter_key(_iterator!.Handle, out var kLen);
        _key = new Ptr((byte*)kPtr, (int)kLen);
        return descriptor.ShouldContinue(_key.Span, useHistory);
    }

    private void Setup<TSliceDescriptor>(TSliceDescriptor descriptor, bool useHistory) 
        where TSliceDescriptor : ISliceDescriptor, allows ref struct
    {
        Debug.Assert(_iterator == null);
        if (!descriptor.IsTotalOrdered)
        {
            _iterator = db.NewIterator(null, globalReadOptions);
        }
        else
        {
            _readOptions = new ReadOptions()
                .SetTotalOrderSeek(true)
                .SetSnapshot(snapshot)
                .SetPinData(false);
            _iterator = db.NewIterator(null, _readOptions);
        }
        descriptor.Reset(this, useHistory);
    }


    public KeyPrefix Prefix => _key.Read<KeyPrefix>(0);
    public Ptr Current => _key;
    public Ptr ValueSpan => _key.SliceFast(KeyPrefix.Size);

    public unsafe Ptr ExtraValueSpan
    {
        get 
        {
            var vPtr = Native.Instance.rocksdb_iter_value(_iterator!.Handle, out var vLen);
            return new Ptr((byte*)vPtr, (int)vLen);
        }
    }
    public void SeekTo(ReadOnlySpan<byte> span) => 
        _iterator!.Seek(span);
    public void Next() => 
        _iterator!.Next();
}
