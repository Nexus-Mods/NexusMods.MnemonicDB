using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes;
using NexusMods.EventSourcing.Storage.Columns.BlobColumns;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;

namespace NexusMods.EventSourcing.Storage.Nodes.Index;

public partial class IndexNode : INode
{
    public record ChildInfo(StoreKey Key, Datom LastDatom, long DeepLength);
    public static IndexNode Create(List<ChildInfo> infos)
    {
        var node = new IndexNode()
        {
            EntityIds = ULongColumn.Create(infos.Count),
            AttributeIds = ULongColumn.Create(infos.Count),
            Values = BlobColumn.Create(),
            TransactionIds = ULongColumn.Create(infos.Count),
            ChildCounts = ULongColumn.Create(infos.Count),
            ChildOffsets = ULongColumn.Create(infos.Count),
            ChildKeys = ULongColumn.Create(infos.Count),
            ShallowLength = infos.Count,
            DeepLength = 0
        };

        long offset = 0;
        foreach (var child in infos)
        {
            node.EntityIds.Add(child.LastDatom.E.Value);
            node.AttributeIds.Add(child.LastDatom.A.Value);
            node.TransactionIds.Add(child.LastDatom.T.Value);
            node.ChildCounts.Add((ulong)child.DeepLength);
            node.ChildOffsets.Add((ulong)offset);
            node.ChildKeys.Add(child.Key.Value);
            offset += child.DeepLength;

        }

        node.DeepLength = offset;



        return node;
    }

    /// <summary>
    /// Create an index node that is the subset of the given node.
    /// </summary>
    public static IndexNode Create(IndexNode infos, long offset, long size)
    {
        var newNode = new IndexNode
        {
            EntityIds = ULongColumn.Create((int)size),
            AttributeIds = ULongColumn.Create((int)size),
            Values = BlobColumn.Create(),
            TransactionIds = ULongColumn.Create((int)size),
            ChildCounts = ULongColumn.Create((int)size),
            ChildOffsets = ULongColumn.Create((int)size),
            ChildKeys = ULongColumn.Create((int)size),
            DeepLength = 0,
            ShallowLength = 0
        };

        for (var i = 0; i < size; i++)
        {
            var idx = (int)(i + offset);
            newNode.EntityIds.Add(infos.EntityIds[idx]);
            newNode.AttributeIds.Add(infos.AttributeIds[idx]);
            newNode.TransactionIds.Add(infos.TransactionIds[idx]);
            newNode.ChildCounts.Add(infos.ChildCounts[idx]);
            newNode.ChildOffsets.Add((ulong)newNode.DeepLength);
            newNode.ChildKeys.Add(infos.ChildKeys[idx]);
            newNode.DeepLength += (long)infos.ChildCounts[idx];
            newNode.ShallowLength++;
        }
        return newNode;
    }


    protected void OnFlatSharpDeserialized()
    {

    }

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

    public IDatomResult All()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the last datom marker for the given child index.
    /// </summary>
    public Datom GetLastDatom(int i)
    {
        if (i == ShallowLength - 1)
        {
            return Datom.Max;
        }

        return new Datom
        {
            E = EntityId.From(EntityIds[i]),
            A = AttributeId.From(AttributeIds[i]),
            T = TxId.From(TransactionIds[i]),
            V = Values.GetMemory(i)
        };

    }

}
