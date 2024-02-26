using System;

namespace NexusMods.EventSourcing.Storage.Abstractions.Comparators;

public class EATV : IDatomComparator
{
    public int Compare(in Datom a, in Datom b)
    {
        var cmp = a.E.CompareTo(b.E);
        if (cmp != 0) return cmp;

        cmp = a.A.CompareTo(b.A);
        if (cmp != 0) return cmp;

        cmp = a.T.CompareTo(b.T);
        if (cmp != 0) return cmp;

        throw new NotImplementedException();

    }
}
