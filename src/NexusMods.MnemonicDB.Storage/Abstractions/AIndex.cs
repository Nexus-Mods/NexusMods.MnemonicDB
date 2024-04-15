using System;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Storage.Abstractions;

public abstract class AIndex<TComparator, TIndexStore>(TIndexStore store) : IIndex
    where TComparator : IDatomComparator
    where TIndexStore : class, IIndexStore
{
    public void Put(IWriteBatch batch, ReadOnlySpan<byte> datom)
    {
        batch.Add(store, datom);
    }

    public void Delete(IWriteBatch batch, ReadOnlySpan<byte> datom)
    {
        batch.Delete(store, datom);
    }
}
