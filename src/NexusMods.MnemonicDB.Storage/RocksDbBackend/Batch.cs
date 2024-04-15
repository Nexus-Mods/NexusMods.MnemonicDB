using System;
using NexusMods.MnemonicDB.Storage.Abstractions;
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

    public void Add(IIndexStore store, ReadOnlySpan<byte> key)
    {
        _batch.Put(key, ReadOnlySpan<byte>.Empty, ((IRocksDBIndexStore)store).Handle);
    }

    public void Delete(IIndexStore store, ReadOnlySpan<byte> key)
    {
        _batch.Delete(key, ((IRocksDBIndexStore)store).Handle);
    }

    public void Commit()
    {
        db.Write(_batch);
    }
}
