using System;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public abstract class AHistoryIndex<TA, TB, TC, TD, TE, TIndexStore>(AttributeRegistry registry, TIndexStore store) : IIndex<TIndexStore>
where TA : IElementComparer
where TB : IElementComparer
where TC : IElementComparer
where TD : IElementComparer
where TE : IElementComparer
where TIndexStore : IIndexStore
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

    public void Assert<TWriteBatch>(TWriteBatch batch, ReadOnlySpan<byte> datom) where TWriteBatch : IWriteBatch<TIndexStore>
    {
        batch.Add(store, datom);
    }

    public void Retract<TWriteBatch>(TWriteBatch batch, ReadOnlySpan<byte> datom, ReadOnlySpan<byte> previousDatom)
        where TWriteBatch : IWriteBatch<TIndexStore>
    {
        batch.Add(store, datom);
    }
}
