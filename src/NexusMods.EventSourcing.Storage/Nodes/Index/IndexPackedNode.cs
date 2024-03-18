using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.Index;

namespace NexusMods.EventSourcing.Storage.Nodes.Index;

public partial class IndexPackedNode : IReadable
{
    protected void OnFlatSharpDeserialized()
    {

    }


    internal List<EventSourcing.Abstractions.Nodes.Data.IReadable> Children = null!;

    public IEnumerator<Datom> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Length => (int)ShallowLength;

    public Datom this[int idx] => throw new NotImplementedException();

    public Datom LastDatom { get; }
    public EntityId GetEntityId(int idx)
    {
        return EntityId.From(EntityIds[idx]);
    }

    public AttributeId GetAttributeId(int idx)
    {
        return AttributeId.From(AttributeIds[idx]);
    }

    public TxId GetTransactionId(int idx)
    {
        return TxId.From(TransactionIds[idx]);
    }

    public ReadOnlySpan<byte> GetValue(int idx)
    {
        return Values[idx];
    }

    public int FillChunk(int offset, int length, ref DatomChunk chunk)
    {
        throw new NotImplementedException();
    }

    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable EntityIdsColumn => EntityIds;
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable AttributeIdsColumn => AttributeIds;
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable TransactionIdsColumn => TransactionIds;
    public EventSourcing.Abstractions.Columns.BlobColumns.IReadable ValuesColumn => Values;
    public long GetChildCount(int idx)
    {
        return (long)ChildCounts[idx];
    }

    public long GetChildOffset(int idx)
    {
        return (long)ChildOffsets[idx];
    }

    public EventSourcing.Abstractions.Nodes.Data.IReadable GetChild(int idx)
    {
        return Children[idx];
    }

    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable ChildCountsColumn => ChildCounts;
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable ChildOffsetsColumn => ChildOffsets;
    public IDatomComparator Comparator { get; private set; } = null!;
}
