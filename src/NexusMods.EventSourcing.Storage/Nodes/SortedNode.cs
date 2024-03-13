using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Columns;
using NexusMods.EventSourcing.Storage.Nodes.DataNode;

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
    public override long DeepLength => indexes.Length;
    public override Datom this[int idx] => inner[indexes[idx]];

    public override Datom LastDatom => inner[indexes[^1]];

    public override void WriteTo<TWriter>(TWriter writer)
    {
        throw new System.NotSupportedException();
    }

    public override IDataNode Flush(INodeStore store)
    {
        throw new NotSupportedException();
    }

    public override EntityId GetEntityId(int idx)
    {
        return inner.GetEntityId(indexes[idx]);
    }

    public override AttributeId GetAttributeId(int idx)
    {
        return inner.GetAttributeId(indexes[idx]);
    }

    public override TxId GetTransactionId(int idx)
    {
        return inner.GetTransactionId(indexes[idx]);
    }

    public override ReadOnlySpan<byte> GetValue(int idx)
    {
        return inner.GetValue(indexes[idx]);
    }
}
