using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;

namespace NexusMods.EventSourcing.Storage.Sorters;

public class AETV(AttributeRegistry registry) : IDatomComparator
{
    public SortOrders SortOrder => SortOrders.AETV;

    public int Compare(in Datom x, in Datom y)
    {
        var cmp = x.A.CompareTo(y.A);
        if (cmp != 0) return cmp;

        cmp = x.E.CompareTo(y.E);
        if (cmp != 0) return cmp;

        cmp = x.T.CompareTo(y.T);
        if (cmp != 0) return -cmp;

        return registry.CompareValues(x, y);
    }

    public IComparer<int> MakeComparer<TBlob>(MemoryDatom<TBlob> datoms) where TBlob : IBlobColumn
    {
        return new AETVComparer<TBlob>(registry, datoms);
    }
}


internal unsafe class AETVComparer<TBlob>(AttributeRegistry registry, MemoryDatom<TBlob> datoms) : IComparer<int>
    where TBlob : IBlobColumn
{

    public int Compare(int a, int b)
    {
        var cmp = datoms.AttributeIds[a].CompareTo(datoms.AttributeIds[b]);
        if (cmp != 0) return cmp;

        cmp = datoms.EntityIds[a].CompareTo(datoms.EntityIds[b]);
        if (cmp != 0) return cmp;

        // Reverse the comparison of transaction ids to get the latest transaction first
        cmp = datoms.TransactionIds[a].CompareTo(datoms.TransactionIds[b]);
        if (cmp != 0) return -cmp;

        return registry.CompareValues(datoms.Values, datoms.AttributeIds[a], a, b);
    }
}
