using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;
using RocksDbSharp;
using IWriteBatch = NexusMods.MnemonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal sealed class WriteBatchWithIndex : ADatomsIndex<WriteBatchWithIndexEnumerator>,
        IRefDatomEnumeratorFactory<WriteBatchWithIndexEnumerator>, IWriteBatch, ISnapshot
{
    private readonly RocksDbSharp.WriteBatchWithIndex _batch;
    private readonly PooledMemoryBufferWriter _writer = new();
    private readonly Snapshot _baseSnapshot;

    public WriteBatchWithIndex(Snapshot baseSnapshot, AttributeCache cache) : base(cache)
    {
        _baseSnapshot = baseSnapshot;
        var ctor = typeof(RocksDbSharp.WriteBatchWithIndex).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] {typeof(IntPtr)});
        var handle = Native.Instance.rocksdb_writebatch_wi_create_with_params(NativeComparators.ComparatorPtr, 0, true, 0, 0);
        _batch = (RocksDbSharp.WriteBatchWithIndex)ctor!.Invoke([handle]);
            
            
    }


    public void Dispose()
    {
        _writer.Dispose();
    }


    public override WriteBatchWithIndexEnumerator GetRefDatomEnumerator()
    {
        return new WriteBatchWithIndexEnumerator(_baseSnapshot.Backend.Db!, _batch, _baseSnapshot.NativeSnapshot,
            _baseSnapshot.ReadOptions);
    }

    public void Commit()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public void Add(Datom datom)
    {
        if (datom.Prefix.ValueTag == ValueTag.HashedBlob)
        {
            var value = (Memory<byte>)datom.Value;
            var outOfBandData = value.Span.SliceFast(Serializer.HashedBlobHeaderSize);
            Span<byte> keySpan = stackalloc byte[Serializer.HashedBlobPrefixSize];

            MemoryMarshal.Write(keySpan, datom.Prefix);
            value.Span.SliceFast(0, Serializer.HashedBlobHeaderSize)
                .CopyTo(keySpan.SliceFast(KeyPrefix.Size));
            _batch.Put(keySpan, outOfBandData);
        }
        else
        {
            _writer.Reset();
            _writer.WriteMarshal(datom.Prefix);
            datom.Prefix.ValueTag.Write(datom.Value, _writer);
            _batch.Put(_writer.GetWrittenSpan(), ReadOnlySpan<byte>.Empty);
        }
    }

    public void Delete(Datom datom)
    {
        if (datom.Prefix.ValueTag == ValueTag.HashedBlob)
        {
            Span<byte> keySpan = stackalloc byte[Serializer.HashedBlobPrefixSize];
            var value = (Memory<byte>)datom.Value;
            MemoryMarshal.Write(keySpan, datom.Prefix);
            value.Span.SliceFast(0, Serializer.HashedBlobHeaderSize).CopyTo(keySpan.SliceFast(KeyPrefix.Size));
            _batch.Delete(keySpan);
        }
        else
        {
            _writer.Reset();
            _writer.WriteMarshal(datom.Prefix);
            datom.Prefix.ValueTag.Write(datom.Value, _writer);
            _batch.Delete(_writer.GetWrittenSpan());
        }
    }

    public IDb MakeDb(TxId txId, AttributeCache attributeCache, IConnection? connection = null)
    {
        return new Db<WriteBatchWithIndex, WriteBatchWithIndexEnumerator>(this, txId, attributeCache, connection);
    }

    public bool TryGetMaxIdInPartition(PartitionId partitionId, out EntityId id)
    {
        throw new NotSupportedException("Cannot get the max Id for a partition in a AsIf database");
    }

    public ISnapshot AsIf(Datoms datoms)
    {
        throw new NotSupportedException("Cannot (currently) chain AsIf databases");
    }
}

internal struct WriteBatchWithIndexEnumerator(RocksDb db, RocksDbSharp.WriteBatchWithIndex batch, RocksDbSharp.Snapshot snapshot, ReadOptions globalReadOptions) 
    : ILowLevelIterator, IRefDatomEnumerator
{
    private Ptr _key;
    private Iterator? _iterator = null;
    private Iterator? _baseIterator = null;
    private RocksDbSharp.ReadOptions? _readOptions = null;

    public void Dispose()
    {
        _iterator?.Dispose();
        _baseIterator?.Dispose();
    }

    public unsafe bool MoveNext<TSliceDescriptor>(TSliceDescriptor descriptor, bool useHistory = false) 
        where TSliceDescriptor : ISliceDescriptor, allows ref struct
    {
        if (_iterator == null)
            Setup(descriptor, useHistory);
        else
            _iterator!.Next();

        if (!_iterator!.Valid())
        {
            _iterator?.Dispose();
            _iterator = null;
            return false;
        }

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
            _baseIterator = db.NewIterator(null, globalReadOptions);
            _iterator = batch.CreateIteratorWithBase(_baseIterator);
        }
        else
        {
            _readOptions = new ReadOptions()
                .SetTotalOrderSeek(true)
                .SetSnapshot(snapshot)
                .SetPinData(false);
            _baseIterator = db.NewIterator(null, _readOptions);
            _iterator = batch.CreateIteratorWithBase(_baseIterator);
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
