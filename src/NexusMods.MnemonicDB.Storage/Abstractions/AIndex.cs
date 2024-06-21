using System;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Storage.Abstractions;

public abstract class AIndex<TComparator, TIndexStore>(TIndexStore store) : IIndex
    where TComparator : IDatomComparator
    where TIndexStore : class, IIndexStore
{
    /// <inheritdoc />
    public void Put(IWriteBatch batch, in Datom datom)
    {

    }

    /// <inheritdoc />
    public void Put(IWriteBatch batch, ReadOnlySpan<byte> datom)
    {
        batch.Add(store, datom);
    }

    public void Delete(IWriteBatch batch, in Datom datom)
    {
        throw new NotImplementedException();
    }

    public void Delete(IWriteBatch batch, ReadOnlySpan<byte> datom)
    {
        batch.Delete(store, datom);
    }
}
