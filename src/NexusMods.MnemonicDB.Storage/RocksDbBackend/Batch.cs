using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;
using IWriteBatch = NexusMods.MnemonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public class Batch(RocksDb db) : IWriteBatch
{
    private readonly WriteBatch _batch = new();

    /// <inheritdoc />
    public void Dispose()
    {
        _batch.Dispose();
    }

    private ValueTags Tag(ReadOnlySpan<byte> key)
    {
        var prefix = MemoryMarshal.Read<KeyPrefix>(key);
        return prefix.ValueTag;
    }

    /// <inheritdoc />
    public void Add(IIndexStore store, ReadOnlySpan<byte> key)
    {
        var outOfBandData = ReadOnlySpan<byte>.Empty;
        if (Tag(key) == ValueTags.HashedBlob)
        {
            outOfBandData = key.SliceFast(KeyPrefix.Size + sizeof(ulong));
            key = key.SliceFast(0, KeyPrefix.Size + sizeof(ulong));
        }

        _batch.Put(key, outOfBandData, ((IRocksDBIndexStore)store).Handle);
    }

    /// <inheritdoc />
    public void Add(IIndexStore store, in Datom datom)
    {
        if (datom.Prefix.ValueTag == ValueTags.HashedBlob)
        {
            var outOfBandData = datom.ValueSpan.SliceFast(sizeof(ulong));
            Span<byte> keySpan = stackalloc byte[KeyPrefix.Size + sizeof(ulong)];

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.SliceFast(0, sizeof(ulong)).CopyTo(keySpan.SliceFast(KeyPrefix.Size));
            _batch.Put(keySpan, outOfBandData, ((IRocksDBIndexStore)store).Handle);
        }
        else if (datom.ValueSpan.Length < 256)
        {
            Span<byte> keySpan = stackalloc byte[KeyPrefix.Size + datom.ValueSpan.Length];

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.CopyTo(keySpan.SliceFast(KeyPrefix.Size));

            _batch.Put(keySpan, ReadOnlySpan<byte>.Empty, ((IRocksDBIndexStore)store).Handle);
        }
        else
        {
            var keySpan = GC.AllocateUninitializedArray<byte>(KeyPrefix.Size + datom.ValueSpan.Length).AsSpan();

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.CopyTo(keySpan[KeyPrefix.Size..]);

            _batch.Put(keySpan, ReadOnlySpan<byte>.Empty, ((IRocksDBIndexStore)store).Handle);
        }
    }

    /// <inheritdoc />
    public void Delete(IIndexStore store, ReadOnlySpan<byte> key)
    {
        if (Tag(key) == ValueTags.HashedBlob)
        {
            key = key.SliceFast(0, KeyPrefix.Size + sizeof(ulong));
        }

        _batch.Delete(key, ((IRocksDBIndexStore)store).Handle);
    }

    /// <inheritdoc />
    public void Delete(IIndexStore store, in Datom datom)
    {
        if (datom.Prefix.ValueTag == ValueTags.HashedBlob)
        {
           Span<byte> keySpan = stackalloc byte[KeyPrefix.Size + sizeof(ulong)];

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.SliceFast(0, sizeof(ulong)).CopyTo(keySpan.SliceFast(KeyPrefix.Size));
            _batch.Delete(keySpan, ((IRocksDBIndexStore)store).Handle);
        }
        else if (datom.ValueSpan.Length < 256)
        {
            Span<byte> keySpan = stackalloc byte[KeyPrefix.Size + datom.ValueSpan.Length];

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.CopyTo(keySpan.SliceFast(KeyPrefix.Size));

            _batch.Put(keySpan, ReadOnlySpan<byte>.Empty, ((IRocksDBIndexStore)store).Handle);
        }
        else
        {
            var keySpan = GC.AllocateUninitializedArray<byte>(KeyPrefix.Size + datom.ValueSpan.Length).AsSpan();

            MemoryMarshal.Write(keySpan, datom.Prefix);
            datom.ValueSpan.CopyTo(keySpan[KeyPrefix.Size..]);

            _batch.Put(keySpan, ReadOnlySpan<byte>.Empty, ((IRocksDBIndexStore)store).Handle);
        }

    }

    /// <inheritdoc />
    public void Commit()
    {
        db.Write(_batch);
    }
}
