using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Sorters;

namespace NexusMods.EventSourcing.Storage;

public interface IDatomComparator
{
    public int Compare(in Datom x, in Datom y);

    public int Compare<T>(in MemoryDatom<T> chunk, int a, int b)
    where T : IBlobColumn;

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
