using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Sorters;

public class EATV(AttributeRegistry registry) : IDatomComparator
{
    public IComparer<int> MakeComparer<TBlob>(MemoryDatom<TBlob> datoms)
        where TBlob : IBlobColumn
    {
        return new EATVComparer<TBlob>(registry, datoms);
    }

    public SortOrders SortOrder => SortOrders.EATV;

    public int Compare(in Datom x, in Datom y)
    {
        var cmp = x.E.CompareTo(y.E);
        if (cmp != 0) return cmp;

        cmp = x.A.CompareTo(y.A);
        if (cmp != 0) return cmp;

        cmp = x.T.CompareTo(y.T);
        if (cmp != 0) return -cmp;

        return registry.CompareValues(x, y);
    }
}


internal unsafe class EATVComparer<TBlob>(AttributeRegistry registry, MemoryDatom<TBlob> datoms) : IComparer<int>
    where TBlob : IBlobColumn
{
    public int Compare(int a, int b)
    {
        var cmp = datoms.EntityIds[a].CompareTo(datoms.EntityIds[b]);
        if (cmp != 0) return cmp;

        cmp = datoms.AttributeIds[a].CompareTo(datoms.AttributeIds[b]);
        if (cmp != 0) return cmp;

        cmp = datoms.TransactionIds[a].CompareTo(datoms.TransactionIds[b]);
        if (cmp != 0) return -cmp;

        return registry.CompareValues(datoms.Values, datoms.AttributeIds[a], a, b);
    }
}
