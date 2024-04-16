using System;
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
        if (key.Length < KeyPrefix.Size + 1)
            return ValueTags.Null;
        return (ValueTags)key[KeyPrefix.Size];
    }

    public void Add(IIndexStore store, ReadOnlySpan<byte> key)
    {
        var outOfBandData = ReadOnlySpan<byte>.Empty;
        if (Tag(key) == ValueTags.HashedBlob)
        {
            outOfBandData = key.SliceFast(KeyPrefix.Size + 1 + sizeof(ulong));
            key = key.SliceFast(0, KeyPrefix.Size + 1 + sizeof(ulong));
        }

        _batch.Put(key, outOfBandData, ((IRocksDBIndexStore)store).Handle);
    }

    public void Delete(IIndexStore store, ReadOnlySpan<byte> key)
    {
        if (Tag(key) == ValueTags.HashedBlob)
        {
            key = key.SliceFast(0, KeyPrefix.Size + 1 + sizeof(ulong));
        }

        _batch.Delete(key, ((IRocksDBIndexStore)store).Handle);
    }

    public void Commit()
    {
        db.Write(_batch);
    }
}
