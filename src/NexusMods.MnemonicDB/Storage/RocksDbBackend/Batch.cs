using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;
using RocksDbSharp;
using IWriteBatch = NexusMods.MnemonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

internal class Batch(RocksDb db) : IWriteBatch
{
    private readonly WriteBatch _batch = new();
    private PooledMemoryBufferWriter _writer = new();

    /// <inheritdoc />
    public void Dispose()
    {
        _writer.Dispose();
        _batch.Dispose();
    }
    
    
    /// <inheritdoc />
    public void Add(ValueDatom datom)
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

    public void Add(Datom datom)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Delete(Datom datom)
    {
        if (datom.Prefix.ValueTag == ValueTag.HashedBlob)
        {
            Span<byte> keySpan = stackalloc byte[Serializer.HashedBlobPrefixSize];

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.SliceFast(0, Serializer.HashedBlobHeaderSize).CopyTo(keySpan.SliceFast(KeyPrefix.Size));
            _batch.Delete(keySpan);
        }
        else if (datom.ValueSpan.Length < 256)
        {
            Span<byte> keySpan = stackalloc byte[KeyPrefix.Size + datom.ValueSpan.Length];

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.CopyTo(keySpan.SliceFast(KeyPrefix.Size));

            _batch.Delete(keySpan);
        }
        else
        {
            var keySpan = GC.AllocateUninitializedArray<byte>(KeyPrefix.Size + datom.ValueSpan.Length).AsSpan();

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.CopyTo(keySpan[KeyPrefix.Size..]);

            _batch.Delete(keySpan);
        }

    }

    /// <inheritdoc />
    public void Commit()
    {
        db.Write(_batch);
    }
}
