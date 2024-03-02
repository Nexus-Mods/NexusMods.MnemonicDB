using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Sorters;

/// <summary>
/// The Txlog is essentially a index of [TxId, EntityId, AttributeId, Value] tuples.
/// </summary>
/// <param name="registry"></param>
public class TxLog(AttributeRegistry registry) : IDatomComparator
{
    public SortOrders SortOrder => SortOrders.TxLog;

    public int Compare(in Datom x, in Datom y)
    {
        var cmp = x.T.CompareTo(y.T);
        if (cmp != 0) return cmp;

        cmp = x.E.CompareTo(y.E);
        if (cmp != 0) return cmp;

        cmp = x.A.CompareTo(y.A);
        if (cmp != 0) return cmp;

        return registry.CompareValues(x, y);
    }

    public IComparer<int> MakeComparer<TBlob>(MemoryDatom<TBlob> datoms) where TBlob : IBlobColumn
    {
        return new TxLogComparer<TBlob>(registry, datoms);
    }

    public int Compare<T>(in MemoryDatom<T> chunk, int a, int b) where T : IBlobColumn
    {
        throw new System.NotImplementedException();
    }
}

internal unsafe class TxLogComparer<TBlob>(AttributeRegistry registry, MemoryDatom<TBlob> datoms) : IComparer<int>
    where TBlob : IBlobColumn
{
    public int Compare(int a, int b)
    {
        var cmp = datoms.TransactionIds[a].CompareTo(datoms.TransactionIds[b]);
        if (cmp != 0) return cmp;

        cmp = datoms.EntityIds[a].CompareTo(datoms.EntityIds[b]);
        if (cmp != 0) return cmp;

        cmp = datoms.AttributeIds[a].CompareTo(datoms.AttributeIds[b]);
        if (cmp != 0) return cmp;

        return registry.CompareValues(datoms.Values, datoms.AttributeIds[a], a, b);
    }
}
