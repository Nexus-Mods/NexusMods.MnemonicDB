using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Columns;

namespace NexusMods.EventSourcing.Storage.Nodes;

/// <summary>
/// Represents a sorted view of another node, this is most often used as a temporary view of a
/// node before it is merged into another node.
/// </summary>
/// <param name="indexes"></param>
/// <param name="inner"></param>
public class SortedNode(int[] indexes, IDataNode inner) : ADataNode
{
    public override IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < indexes.Length; i++)
        {
            yield return inner[indexes[i]];
        }
    }

    public override int Length => indexes.Length;
    public override IColumn<EntityId> EntityIds => new SortedColumn<EntityId>(indexes, inner.EntityIds);
    public override IColumn<AttributeId> AttributeIds => new SortedColumn<AttributeId>(indexes, inner.AttributeIds);
    public override IColumn<TxId> TransactionIds => new SortedColumn<TxId>(indexes, inner.TransactionIds);
    public override IColumn<DatomFlags> Flags => new SortedColumn<DatomFlags>(indexes, inner.Flags);
    public override IBlobColumn Values => new SortedBlobColumn(indexes, inner.Values);

    public override Datom this[int idx] => throw new System.NotImplementedException();

    public override Datom LastDatom { get; }

    public override void WriteTo<TWriter>(TWriter writer)
    {
        throw new System.NotSupportedException();
    }

    public override IDataNode Flush(INodeStore store)
    {
        throw new NotSupportedException();
    }
}
