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
        throw new NotImplementedException();
    }

    public AttributeId GetAttributeId(long idx)
    {
        throw new NotImplementedException();
    }

    public TxId GetTransactionId(long idx)
    {
        throw new NotImplementedException();
    }

    public ReadOnlySpan<byte> GetValue(long idx)
    {
        throw new NotImplementedException();
    }

    public ReadOnlyMemory<byte> GetValueMemory(long idx)
    {
        throw new NotImplementedException();
    }
}
