using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Sorters;

public class EATV(AttributeRegistry registry) : IDatomComparator
{
    public int Compare(in AppendableChunk chunk, int a, int b)
    {
        var cmp = chunk.EntityIds[a].CompareTo(chunk.EntityIds[b]);
        if (cmp != 0) return cmp;

        cmp = chunk.AttributeIds[a].CompareTo(chunk.AttributeIds[b]);
        if (cmp != 0) return cmp;

        cmp = chunk.TransactionIds[a].CompareTo(chunk.TransactionIds[b]);
        if (cmp != 0) return cmp;

        var datomA = chunk[a];
        var datomB = chunk[b];

        return registry.CompareValues(datomA, datomB);
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
