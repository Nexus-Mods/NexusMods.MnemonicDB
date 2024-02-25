﻿using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Sorters;

namespace NexusMods.EventSourcing.Storage;

public interface IDatomComparator
{
    public int Compare<TDatomA, TDatomB>(in TDatomA x, in TDatomB y)
    where TDatomA : IRawDatom
    where TDatomB : IRawDatom;

    public static IDatomComparator Create(SortOrders sortOrder, AttributeRegistry registry)
    {
        return sortOrder switch
        {
            SortOrders.EATV => new EATV(registry),
            SortOrders.AETV => new AETV(registry),
            _ => throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, null)
        };
    }
}
