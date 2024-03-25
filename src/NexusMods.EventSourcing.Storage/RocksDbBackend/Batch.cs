using System;
using NexusMods.EventSourcing.Storage.Abstractions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.RocksDbBackend;
public class Batch(RocksDbSharp.RocksDb db) : IWriteBatch<IndexStore>
{
    private readonly WriteBatch _batch = new();

    public void Dispose()
    {
        _batch.Dispose();
    }

    public void Add(IndexStore store, ReadOnlySpan<byte> key)
    {
        _batch.Put(key, ReadOnlySpan<byte>.Empty, store.Handle);
    }

    public void Delete(IndexStore store, ReadOnlySpan<byte> key)
    {
        _batch.Delete(key, store.Handle);
    }

    public void Commit()
    {
        db.Write(_batch);
    }
}
