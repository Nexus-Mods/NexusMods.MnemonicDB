using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Sorters;

public class AVTE(AttributeRegistry registry) : IDatomComparator
{
    public SortOrders SortOrder => SortOrders.AVTE;

    public int Compare(in Datom x, in Datom y)
    {
        var cmp = x.A.CompareTo(y.A);
        if (cmp != 0) return cmp;

        cmp = registry.CompareValues(x, y);
        if (cmp != 0) return cmp;

        cmp = x.T.CompareTo(y.T);
        if (cmp != 0) return -cmp;

        return x.E.CompareTo(y.E);
    }

    public IComparer<int> MakeComparer<TBlob>(MemoryDatom<TBlob> datoms) where TBlob : IBlobColumn
    {
        return new AVTEComparer<TBlob>(registry, datoms);
    }
}


internal unsafe class AVTEComparer<TBlob>(AttributeRegistry registry, MemoryDatom<TBlob> datoms) : IComparer<int>
    where TBlob : IBlobColumn
{

    public int Compare(int a, int b)
    {
        var cmp = datoms.AttributeIds[a].CompareTo(datoms.AttributeIds[b]);
        if (cmp != 0) return cmp;

        cmp = registry.CompareValues(datoms.Values, datoms.AttributeIds[a], a, b);
        if (cmp != 0) return cmp;

        // Reverse the comparison of transaction ids to get the latest transaction first
        cmp = datoms.TransactionIds[a].CompareTo(datoms.TransactionIds[b]);
        if (cmp != 0) return -cmp;

        return datoms.EntityIds[a].CompareTo(datoms.EntityIds[b]);
    }
}
