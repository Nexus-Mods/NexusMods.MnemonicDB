using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public abstract class AIndex<TA, TB, TC, TD, TE, TIndexStore>(AttributeRegistry registry, TIndexStore store, bool keepHistory) : IIndex
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

    public void Assert(IWriteBatch batch, ReadOnlySpan<byte> datom)
    {
        batch.Add(store, datom);
    }

    public void Retract(IWriteBatch batch, ReadOnlySpan<byte> datom, ReadOnlySpan<byte> previousDatom)
    {
        batch.Add(store, datom);
        if (!keepHistory)
            batch.Delete(store, previousDatom);
    }

    public IDatomIterator GetIterator()
    {
        return store.GetIterator();
    }
}
