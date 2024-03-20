using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Storage.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Nodes.Index;

public partial class IndexContext
{
    public IDatomResult All()
    {
        var node = Store.Get(Root);
        return node switch
        {
            DataNode dataNode => dataNode.All(),
            IndexNode indexNode => new IndexNodeResults(indexNode, Store),
            _ => throw new NotSupportedException()
        };
    }
}

internal class IndexNodeResults(IndexNode root, INodeStore store) : IDatomResult
{
    public long Length => root.DeepLength;
    public void Fill(long offset, DatomChunk chunk)
    {
        throw new NotImplementedException();
    }

    public void FillValue(long offset, DatomChunk chunk, int idx)
    {
        throw new NotImplementedException();
    }

    public EntityId GetEntityId(long idx)
    {
        var dataNode = FindDataNode(root, idx, out var remainder);
        return dataNode.GetEntityId(remainder);
    }

    private DataNode FindDataNode(IndexNode node, long index, out long remainder)
    {
        for (var i = 0; i < node.ShallowLength; i++)
        {
            var offset = (long)node.ChildOffsets[i];
            var childLength = (long)node.ChildCounts[i];
            if (index >= offset && index < offset + childLength)
            {
                remainder = index - offset;
                var nextNode = store.Get(StoreKey.From(node.ChildKeys[i]));
                return nextNode switch
                {
                    DataNode dataNode => dataNode,
                    IndexNode indexNode => FindDataNode(indexNode, remainder, out remainder),
                    _ => throw new NotSupportedException()
                };
            }
        }

        throw new IndexOutOfRangeException();
    }

    public AttributeId GetAttributeId(long idx)
    {
        var dataNode = FindDataNode(root, idx, out var remainder);
        return dataNode.GetAttributeId(remainder);

    }

    public TxId GetTransactionId(long idx)
    {
        var dataNode = FindDataNode(root, idx, out var remainder);
        return dataNode.GetTransactionId(remainder);
    }

    public ReadOnlySpan<byte> GetValue(long idx)
    {
        var dataNode = FindDataNode(root, idx, out var remainder);
        return dataNode.GetValue(remainder);
    }

    public ReadOnlyMemory<byte> GetValueMemory(long idx)
    {
        var dataNode = FindDataNode(root, idx, out var remainder);
        return dataNode.GetValueMemory(remainder);
    }
}
