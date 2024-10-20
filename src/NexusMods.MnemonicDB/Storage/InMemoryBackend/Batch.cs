using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage.Abstractions;
using IWriteBatch = NexusMods.MnemonicDB.Storage.Abstractions.IWriteBatch;

namespace NexusMods.MnemonicDB.Storage.InMemoryBackend;

public class Batch(IndexStore[] stores) : IWriteBatch
{
    private readonly Dictionary<IndexType, List<(bool IsDelete, byte[] Data)>> _datoms = new();

    /// <inheritdoc />
    public void Dispose() { }

    /// <inheritdoc />
    public void Commit()
    {
        foreach (var (index, datoms) in _datoms)
        {
            var store = stores[(int)index];
            store.Commit(datoms);
        }
    }
    
    /// <inheritdoc />
    public void Add(IIndexStore store, in Datom datom)
    {
        if (store is not IndexStore indexStore)
            throw new ArgumentException("Invalid store type", nameof(store));

        if (!_datoms.TryGetValue(indexStore.Type, out var datoms))
        {
            datoms = new List<(bool IsDelete, byte[] Data)>();
            _datoms.Add(indexStore.Type, datoms);
        }

        datoms.Add((false, datom.ToArray()));
    }
    
    /// <inheritdoc />
    public void Delete(IIndexStore store, in Datom datom)
    {
        if (store is not IndexStore indexStore)
            throw new ArgumentException("Invalid store type", nameof(store));

        if (!_datoms.TryGetValue(indexStore.Type, out var datoms))
        {
            datoms = new List<(bool IsDelete, byte[] Data)>();
            _datoms.Add(indexStore.Type, datoms);
        }

        datoms.Add((true, datom.ToArray()));
    }
}
