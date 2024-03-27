using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.DatomIterators;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public abstract class AIndex<TA, TB, TC, TD, TE, TIndexStore>(AttributeRegistry registry, TIndexStore store) : IIndex
where TA : IElementComparer
where TB : IElementComparer
where TC : IElementComparer
where TD : IElementComparer
where TE : IElementComparer
where TIndexStore : class, IIndexStore
{
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var cmp = TA.Compare(registry, a, b);
        if (cmp != 0) return cmp;

        cmp = TB.Compare(registry, a, b);
        if (cmp != 0) return cmp;

        cmp = TC.Compare(registry, a, b);
        if (cmp != 0) return cmp;

        cmp = TD.Compare(registry, a, b);
        if (cmp != 0) return cmp;

        return TE.Compare(registry, a, b);
    }

    public void Put(IWriteBatch batch, ReadOnlySpan<byte> datom)
    {
        batch.Add(store, datom);
    }

    public void Delete(IWriteBatch batch, ReadOnlySpan<byte> datom)
    {
        batch.Delete(store, datom);
    }

    public IDatomSource GetIterator()
    {
        return store.GetIterator();
    }
}
