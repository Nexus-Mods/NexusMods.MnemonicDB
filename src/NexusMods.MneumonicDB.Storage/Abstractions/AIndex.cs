using System;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;

namespace NexusMods.MneumonicDB.Storage.Abstractions;

public abstract class AIndex<TComparator, TIndexStore>(AttributeRegistry registry, TIndexStore store) : IIndex
    where TComparator : IDatomComparator<AttributeRegistry>
    where TIndexStore : class, IIndexStore
{
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return TComparator.Compare(registry, a, b);
    }

    public void Put(IWriteBatch batch, ReadOnlySpan<byte> datom)
    {
        batch.Add(store, datom);
    }

    public void Delete(IWriteBatch batch, ReadOnlySpan<byte> datom)
    {
        batch.Delete(store, datom);
    }
}
