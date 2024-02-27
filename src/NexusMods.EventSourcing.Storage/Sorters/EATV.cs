using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage.Sorters;

public class EATV(AttributeRegistry registry) : IDatomComparator
{
    private ReadOnlyMemory<EntityId> _eids;
    private ReadOnlyMemory<AttributeId> _attributeIds;
    private ReadOnlyMemory<TxId> _transactionIds;

    public void Prep(AppendableChunk chunk)
    {
        _eids = ((UnsignedIntegerColumn<EntityId>)chunk.EntityIds).Memory;
        _attributeIds = ((UnsignedIntegerColumn<AttributeId>)chunk.AttributeIds).Memory;
        _transactionIds = ((UnsignedIntegerColumn<TxId>)chunk.TransactionIds).Memory;
    }

    public int Compare<T>(in MemoryDatom<T> datoms, int a, int b)
    where T : IBlobColumn
    {
        unsafe
        {
            var cmp = datoms.EntityIds[a].CompareTo(datoms.EntityIds[b]);
            if (cmp != 0) return cmp;

            cmp = datoms.AttributeIds[a].CompareTo(datoms.AttributeIds[b]);
            if (cmp != 0) return cmp;

            cmp = datoms.TransactionIds[a].CompareTo(datoms.TransactionIds[b]);
            if (cmp != 0) return cmp;

            return registry.CompareValues(datoms.Values, datoms.AttributeIds[a], a, b);
        }
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
