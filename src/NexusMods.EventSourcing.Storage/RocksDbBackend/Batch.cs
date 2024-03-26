using System;
using NexusMods.EventSourcing.Storage.Abstractions;
using RocksDbSharp;
using IWriteBatch = NexusMods.EventSourcing.Storage.Abstractions.IWriteBatch;

namespace NexusMods.EventSourcing.Storage.RocksDbBackend;
public class Batch(RocksDb db) : IWriteBatch
{
    private readonly WriteBatch _batch = new();

    public void Dispose()
    {
        _batch.Dispose();
    }

    public void Add(IIndexStore store, ReadOnlySpan<byte> key)
    {
        _batch.Put(key, ReadOnlySpan<byte>.Empty, ((IndexStore)store).Handle);
    }

    public void Delete(IIndexStore store, ReadOnlySpan<byte> key)
    {
        _batch.Delete(key, ((IndexStore)store).Handle);
    }

    public void Commit()
    {
        db.Write(_batch);
    }
}
