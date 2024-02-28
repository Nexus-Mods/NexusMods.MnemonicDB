using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Sorters;

public class EATV(AttributeRegistry registry) : IDatomComparator
{

    public unsafe IComparer<int> MakeComparer<TBlob>(MemoryDatom<TBlob> datoms, int* indices)
        where TBlob : IBlobColumn
    {
        return new EATVComparer<TBlob>(registry, datoms, indices);
    }

    public int Compare(in Datom x, in Datom y)
    {
        var cmp = x.E.CompareTo(y.E);
        if (cmp != 0) return cmp;

        cmp = x.A.CompareTo(y.A);
        if (cmp != 0) return cmp;

        cmp = x.T.CompareTo(y.T);
        if (cmp != 0) return cmp;

        return registry.CompareValues(x, y);
    }
}


internal unsafe class EATVComparer<TBlob>(AttributeRegistry registry, MemoryDatom<TBlob> datoms, int* indices) : IComparer<int>
where TBlob : IBlobColumn
{
    public int Compare(int a, int b)
    {
        var cmp = datoms.EntityIds[indices[a]].CompareTo(datoms.EntityIds[indices[b]]);
        if (cmp != 0) return cmp;

        cmp = datoms.AttributeIds[indices[a]].CompareTo(datoms.AttributeIds[indices[b]]);
        if (cmp != 0) return cmp;

        cmp = datoms.TransactionIds[indices[a]].CompareTo(datoms.TransactionIds[indices[b]]);
        if (cmp != 0) return cmp;

        return registry.CompareValues(datoms.Values, datoms.AttributeIds[indices[a]], indices[a], indices[b]);
    }
}
