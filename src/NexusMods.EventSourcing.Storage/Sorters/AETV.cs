using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Sorters;

public class AETV(AttributeRegistry registry) : IDatomComparator
{
    public int Compare(in Datom x, in Datom y)
    {
        var cmp = x.A.CompareTo(y.A);
        if (cmp != 0) return cmp;

        cmp = x.E.CompareTo(y.E);
        if (cmp != 0) return cmp;

        cmp = x.T.CompareTo(y.T);
        if (cmp != 0) return cmp;

        return registry.CompareValues(x, y);
    }

    public unsafe IComparer<int> MakeComparer<TBlob>(MemoryDatom<TBlob> datoms, int* indices) where TBlob : IBlobColumn
    {
        throw new System.NotImplementedException();
    }

    public int Compare<T>(in MemoryDatom<T> chunk, int a, int b) where T : IBlobColumn
    {
        throw new System.NotImplementedException();
    }
}
