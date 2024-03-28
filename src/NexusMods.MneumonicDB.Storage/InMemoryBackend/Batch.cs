using System;
using System.Collections.Generic;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Storage.Abstractions;
using IWriteBatch = NexusMods.MneumonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MneumonicDB.Storage.InMemoryBackend;

public class Batch(IndexStore[] stores) : IWriteBatch
{
    private readonly Dictionary<IndexType, List<(bool IsDelete, byte[] Data)>> _datoms = new();

    public void Dispose() { }

    public void Commit()
    {
        foreach (var (index, datoms) in _datoms)
        {
            var store = stores[(int)index];
            store.Commit(datoms);
        }
    }

    public void Add(IIndexStore store, ReadOnlySpan<byte> key)
    {
        if (store is not IndexStore indexStore)
            throw new ArgumentException("Invalid store type", nameof(store));

        if (!_datoms.TryGetValue(indexStore.Type, out var datoms))
        {
            datoms = new List<(bool IsDelete, byte[] Data)>();
            _datoms.Add(indexStore.Type, datoms);
        }

        datoms.Add((false, key.ToArray()));
    }

    public void Delete(IIndexStore store, ReadOnlySpan<byte> key)
    {
        if (store is not IndexStore indexStore)
            throw new ArgumentException("Invalid store type", nameof(store));

        if (!_datoms.TryGetValue(indexStore.Type, out var datoms))
        {
            datoms = new List<(bool IsDelete, byte[] Data)>();
            _datoms.Add(indexStore.Type, datoms);
        }

        datoms.Add((true, key.ToArray()));
    }
}
