using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class PackedIndexChunk : IIndexChunk
{
    public PackedIndexChunk(int length,
        IColumn<EntityId> entityIds,
        IColumn<AttributeId> attributeIds,
        IColumn<TxId> transactionIds,
        IColumn<DatomFlags> flags,
        IBlobColumn values,
        IColumn<int> childCounts,
        IDatomComparator comparator,
        IEnumerable<IDataChunk> children)
    {
        Length = length;
        EntityIds = entityIds;
        AttributeIds = attributeIds;
        TransactionIds = transactionIds;
        Flags = flags;
        Values = values;
        Children = children;
        ChildCounts = childCounts;
        Comparator = comparator;
    }

    public IEnumerator<Datom> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Length { get; }
    public IColumn<EntityId> EntityIds { get; }
    public IColumn<AttributeId> AttributeIds { get; }
    public IColumn<TxId> TransactionIds { get; }
    public IColumn<DatomFlags> Flags { get; }
    public IBlobColumn Values { get; }

    public Datom this[int idx] => throw new System.NotImplementedException();

    public Datom LastDatom => throw new NotImplementedException();
    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new System.NotImplementedException();
    }

    public IDataChunk Flush(NodeStore store)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<IDataChunk> Children { get; }
    public IColumn<int> ChildCounts { get; }
    public IDatomComparator Comparator { get; }
}
