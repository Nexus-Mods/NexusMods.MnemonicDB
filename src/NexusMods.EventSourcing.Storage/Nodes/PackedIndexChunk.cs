using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Algorithms;
using NexusMods.EventSourcing.Storage.ValueTypes;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class PackedIndexChunk : IIndexChunk
{
    private readonly IColumn<int> _childCounts;
    private readonly List<IDataChunk> _children;

    public PackedIndexChunk(int length,
        IColumn<EntityId> entityIds,
        IColumn<AttributeId> attributeIds,
        IColumn<TxId> transactionIds,
        IColumn<DatomFlags> flags,
        IBlobColumn values,
        IColumn<int> childCounts,
        IDatomComparator comparator,
        List<IDataChunk> children)
    {
        Length = length;
        EntityIds = entityIds;
        AttributeIds = attributeIds;
        TransactionIds = transactionIds;
        Flags = flags;
        Values = values;
        _children = children;
        _childCounts = childCounts;
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

    public Datom this[int idx]
    {
        get
        {
            var acc = 0;
            for (var j = 0; j < _children.Count; j++)
            {
                var childSize = _childCounts[j];
                if (idx < acc + _childCounts[j])
                {
                    return _children[j][idx - acc];
                }
                acc += childSize;
            }
            throw new IndexOutOfRangeException();
        }
    }

    public Datom LastDatom => throw new NotImplementedException();
    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        writer.WriteFourCC(FourCC.PackedIndex);
        writer.Write(Length);
        writer.Write(_childCounts.Length);
        EntityIds.WriteTo(writer);
        AttributeIds.WriteTo(writer);
        TransactionIds.WriteTo(writer);
        Flags.WriteTo(writer);
        Values.WriteTo(writer);
        _childCounts.WriteTo(writer);
        writer.Write((byte)Comparator.SortOrder);
        foreach (var child in _children)
        {
            if (child is ReferenceChunk indexChunk)
            {
                writer.WriteFourCC(FourCC.ReferenceIndex);
                writer.Write((ulong)indexChunk.Key);
            }
            else if (child is ReferenceChunk dataChunk)
            {
                writer.WriteFourCC(FourCC.ReferenceData);
                writer.Write((ulong)dataChunk.Key);
            }
            else
            {
                throw new NotSupportedException("Unknown child type: " + child.GetType().Name);
            }
        }
    }


    public static PackedIndexChunk ReadFrom(ref BufferReader reader, NodeStore nodeStore, AttributeRegistry registry)
    {
        var length = reader.Read<int>();
        var childCount = reader.Read<int>();
        var entityIds = ColumnReader.ReadColumn<EntityId>(ref reader, childCount - 1);
        var attributeIds = ColumnReader.ReadColumn<AttributeId>(ref reader, childCount - 1);
        var transactionIds = ColumnReader.ReadColumn<TxId>(ref reader, childCount - 1);
        var flags = ColumnReader.ReadColumn<DatomFlags>(ref reader, childCount - 1);
        var values = ColumnReader.ReadBlobColumn(ref reader, childCount - 1);
        var childCounts = ColumnReader.ReadColumn<int>(ref reader, childCount);
        var sortOrder = (SortOrders)reader.Read<byte>();
        var comparator = IDatomComparator.Create(sortOrder, registry);

        var children = new List<IDataChunk>();
        for (var i = 0; i < childCount; i++)
        {
            var fourcc = reader.ReadFourCC();
            var key = reader.Read<ulong>();
            if (fourcc == FourCC.ReferenceIndex)
            {
                children.Add(new ReferenceChunk(nodeStore, StoreKey.From(key), null));
            }
            else if (fourcc == FourCC.ReferenceData)
            {
                children.Add(new ReferenceChunk(nodeStore, StoreKey.From(key), null));
            }
            else
            {
                throw new NotSupportedException("Unknown child type: " + fourcc);
            }
        }

        return new PackedIndexChunk(length, entityIds, attributeIds, transactionIds, flags, values, childCounts, comparator, children);
    }

    public IDataChunk Flush(NodeStore store)
    {
        for (var i = 0; i < _children.Count; i++)
        {
            _children[i] = _children[i].Flush(store);
        }

        return this;
    }

    public IEnumerable<IDataChunk> Children => _children;

    public IColumn<int> ChildCounts => _childCounts;

    public IDatomComparator Comparator { get; }

}
