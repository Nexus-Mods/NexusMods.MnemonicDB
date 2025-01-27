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

    /// <inheritdoc />
    public void Dispose()
    {
        _batch.Dispose();
    }
    
    
    /// <inheritdoc />
    public void Add(Datom datom)
    {
        if (datom.Prefix.ValueTag == ValueTag.HashedBlob)
        {
            var outOfBandData = datom.ValueSpan.SliceFast(Serializer.HashedBlobHeaderSize);
            Span<byte> keySpan = stackalloc byte[Serializer.HashedBlobPrefixSize];

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.SliceFast(0, Serializer.HashedBlobHeaderSize).CopyTo(keySpan.SliceFast(KeyPrefix.Size));
            _batch.Put(keySpan, outOfBandData);
        }
        else if (datom.ValueSpan.Length < 256)
        {
            Span<byte> keySpan = stackalloc byte[KeyPrefix.Size + datom.ValueSpan.Length];

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.CopyTo(keySpan.SliceFast(KeyPrefix.Size));

            _batch.Put(keySpan, ReadOnlySpan<byte>.Empty);
        }
        else
        {
            var keySpan = GC.AllocateUninitializedArray<byte>(KeyPrefix.Size + datom.ValueSpan.Length).AsSpan();

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.CopyTo(keySpan[KeyPrefix.Size..]);

            _batch.Put(keySpan, ReadOnlySpan<byte>.Empty);
        }
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
