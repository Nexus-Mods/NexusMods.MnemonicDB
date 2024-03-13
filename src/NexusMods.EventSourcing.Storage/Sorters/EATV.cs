using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Sorters;

public class EATV(AttributeRegistry registry) : IDatomComparator
{
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

    public IComparer<int> MakeComparer(IReadable datoms)
    {
        return new EATVComparer(registry, datoms);
    }

    private class EATVComparer(AttributeRegistry registry, IReadable datoms) : IComparer<int>
    {
        public int Compare(int a, int b)
        {
            var cmp = datoms.GetEntityId(a).CompareTo(datoms.GetEntityId(b));
            if (cmp != 0) return cmp;

            cmp = datoms.GetAttributeId(a).CompareTo(datoms.GetAttributeId(b));
            if (cmp != 0) return cmp;

            cmp = datoms.GetTransactionId(a).CompareTo(datoms.GetTransactionId(b));
            if (cmp != 0) return -cmp;

            return registry.CompareValues(datoms.GetAttributeId(a), datoms.GetValue(a), datoms.GetValue(b));
        }
    }
}



