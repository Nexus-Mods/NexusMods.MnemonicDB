using System;
using System.Runtime.InteropServices;
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

    public void Dispose()
    {
        _batch.Dispose();
    }

    private ValueTags Tag(ReadOnlySpan<byte> key)
    {
        var prefix = MemoryMarshal.Read<KeyPrefix>(key);
        return prefix.ValueTag;
    }

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

    public void Delete(IIndexStore store, ReadOnlySpan<byte> key)
    {
        if (Tag(key) == ValueTags.HashedBlob)
        {
            key = key.SliceFast(0, KeyPrefix.Size + sizeof(ulong));
        }

        _batch.Delete(key, ((IRocksDBIndexStore)store).Handle);
    }

    public void Commit()
    {
        db.Write(_batch);
    }
}
