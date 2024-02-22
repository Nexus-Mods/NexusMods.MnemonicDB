namespace NexusMods.EventSourcing.Storage.Sorters;

public class Eatv<TDatomA, TDatomB> : IDatomComparator<TDatomA, TDatomB>
    where TDatomA : IRawDatom
    where TDatomB : IRawDatom
{
    public int Compare(in TDatomA x, in TDatomB y)
    {
        var cmp = x.EntityId.CompareTo(y.EntityId);
        if (cmp != 0) return cmp;

        cmp = x.AttributeId.CompareTo(y.AttributeId);
        if (cmp != 0) return cmp;

        cmp = x.TxId.CompareTo(y.TxId);
        if (cmp != 0) return cmp;

        return x.ValueLiteral.CompareTo(y.ValueLiteral);
    }
}
