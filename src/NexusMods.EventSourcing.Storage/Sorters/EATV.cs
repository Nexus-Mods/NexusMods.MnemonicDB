﻿using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Sorters;

public class Eatv(AttributeRegistry registry) : IDatomComparator
{
    public int Compare<TDatomA, TDatomB>(in TDatomA x, in TDatomB y)
        where TDatomA : IRawDatom
        where TDatomB : IRawDatom
    {
        var cmp = x.EntityId.CompareTo(y.EntityId);
        if (cmp != 0) return cmp;

        cmp = x.AttributeId.CompareTo(y.AttributeId);
        if (cmp != 0) return cmp;

        cmp = x.TxId.CompareTo(y.TxId);
        if (cmp != 0) return cmp;

        return registry.CompareValues(x, y);
    }
}
