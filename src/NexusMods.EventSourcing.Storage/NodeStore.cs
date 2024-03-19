using System;
using System.Collections.Generic;
using System.Threading;
using FlatSharp;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Nodes;
using NexusMods.EventSourcing.Storage.Nodes.Data;
using NexusMods.EventSourcing.Storage.Nodes.Index;

namespace NexusMods.EventSourcing.Storage;

public class NodeStore(IKvStore kvStore, AttributeRegistry registry)
: INodeStore
{
    private ulong _txLogId = Ids.MinId(Ids.Partition.TxLog);
    private ulong _nextBlockId = Ids.MinId(Ids.Partition.Index);

    /// <summary>
    /// Writes the node to the store as the txLog block, returns the next txId
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public StoreKey LogTx(DataNode node)
    {
        using var writer = new PooledMemoryBufferWriter();


        var thisTx = ++_txLogId;
        Interlocked.Exchange(ref _nextBlockId, Ids.MakeId(Ids.Partition.Index, thisTx << 16));
        _nextBlockId = Ids.MakeId(Ids.Partition.Index, thisTx << 16);
        var logId = Ids.MakeId(Ids.Partition.TxLog, thisTx);

        var key = StoreKey.From(logId);

        throw new NotImplementedException();
    }

    public StoreKey LogTx(INode node)
    {
        throw new NotImplementedException();
    }

    public StoreKey Put(INode node)
    {
        var key = StoreKey.From(Interlocked.Increment(ref _nextBlockId));

        ContainerNode container;
        if (node is DataNode dataNode)
        {
            container = new ContainerNode
            {
                Node = new NodeUnion(dataNode)
            };
        }
        else if (node is IndexNode indexNode)
        {
            container = new ContainerNode
            {
                Node = new NodeUnion(indexNode)
            };
        }
        else
        {
            throw new NotImplementedException();
        }
        container.Id = key.Value;
        container.Timestamp = (ulong)DateTime.UtcNow.Ticks;

        using var writer = new PooledMemoryBufferWriter();
        ContainerNode.Serializer.Write(writer, container);

        kvStore.Put(key, writer.WrittenMemory.Span);

        return key;


    }

    public INode Get(StoreKey key)
    {
        if (!kvStore.TryGet(key, out var span))
        {
            throw new KeyNotFoundException();
        }

        var memory = new byte[span.Length];
        span.CopyTo(memory);

        var container = ContainerNode.Serializer.Parse(new Memory<byte>(memory));

        if (container.Node.Kind == NodeUnion.ItemKind.Data)
        {
            return container.Node.Data;
        }

        if (container.Node.Kind == NodeUnion.ItemKind.Index)
        {
            return container.Node.Index;
        }

        throw new NotImplementedException();
    }

}
