using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;

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

    public IComparer<int> MakeComparer(IReadable datoms)
    {
        return new Comparer(registry, datoms);
    }

    private class Comparer(AttributeRegistry registry, IReadable src) : IComparer<int>
    {
        public int Compare(int a, int b)
        {
            var cmp = src.GetAttributeId(a).CompareTo(src.GetAttributeId(b));
            if (cmp != 0) return cmp;

            cmp = src.GetEntityId(a).CompareTo(src.GetEntityId(b));
            if (cmp != 0) return cmp;

            // Reverse the comparison of transaction ids to get the latest transaction first
            cmp = src.GetTransactionId(a).CompareTo(src.GetTransactionId(b));
            if (cmp != 0) return -cmp;

            return registry.CompareValues(src.GetAttributeId(a), src.GetValue(a), src.GetValue(b));
        }
    }
}



