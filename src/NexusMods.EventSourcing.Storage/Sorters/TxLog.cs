using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Sorters;

/// <summary>
/// The Txlog is essentially a index of [TxId, EntityId, AttributeId, Value] tuples.
/// </summary>
/// <param name="registry"></param>
public class TxLog(AttributeRegistry registry) : IDatomComparator
{
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
}
