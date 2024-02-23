using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Sorters;

public class Avte(AttributeRegistry registry) : IDatomComparator
{
    public int Compare<TDatomA, TDatomB>(in TDatomA x, in TDatomB y)
        where TDatomA : IRawDatom
        where TDatomB : IRawDatom
    {
        var cmp = x.AttributeId.CompareTo(y.AttributeId);
        if (cmp != 0) return cmp;

        cmp = registry.CompareValues(x, y);
        if (cmp != 0) return cmp;

        cmp = x.TxId.CompareTo(y.TxId);
        if (cmp != 0) return cmp;

        return x.EntityId.CompareTo(y.EntityId);
    }
}
