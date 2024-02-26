using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.ValueTypes;

namespace NexusMods.EventSourcing.Storage;

public class NodeStore(ILogger<NodeStore> logger, IKvStore kvStore, Configuration configuration)
{
    private ulong _txLogId = Ids.MinId(Ids.Partition.TxLog);
    private ulong _nextBlockId = Ids.MinId(Ids.Partition.Index);

    /// <summary>
    /// Writes the node to the store as the txLog block, returns the next txId
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public TxId LogTx(OldAppendableNode node)
    {
        var thisTx = ++_txLogId;
        Interlocked.Exchange(ref _nextBlockId, Ids.MakeId(Ids.Partition.Index, thisTx << 16));
        _nextBlockId = Ids.MakeId(Ids.Partition.Index, thisTx << 16);
        var logId = Ids.MakeId(Ids.Partition.TxLog, thisTx);
        Flush(StoreKey.From(logId), node);
        return TxId.From(logId);
    }

    private StoreKey NextBlockId()
    {
        return StoreKey.From(Interlocked.Increment(ref _nextBlockId));
    }

    public ReferenceNode Flush(INode node)
    {
        return node switch
        {
            ReferenceNode referenceNode => referenceNode,
            OldAppendableNode appendableBlock => Flush(appendableBlock),
            IndexNode indexNode => Flush(indexNode),
            _ => throw new NotImplementedException("Unknown node type. " + node.GetType().Name)
        };
    }

    private ReferenceNode Flush(OldAppendableNode oldAppendableNode)
    {
        return Flush(NextBlockId(), oldAppendableNode);
    }

    private ReferenceNode Flush(StoreKey id, OldAppendableNode oldAppendableNode)
    {
        var writer = new PooledMemoryBufferWriter();
        oldAppendableNode.WriteTo(writer);
        var writtenSpan = writer.GetWrittenSpan();


        logger.LogDebug("Flushing index node {Key} with {Count} children of size {Size}", id, oldAppendableNode.ChildCount, writtenSpan.Length);

        kvStore.Put(id, writtenSpan);
        return new ReferenceNode(this)
        {
            Id = id,
            Count = oldAppendableNode.Count,
            ChildCount = oldAppendableNode.Count,
            LastDatom = OnHeapDatom.Create(oldAppendableNode.LastDatom)
        };
    }

    private ReferenceNode Flush(IndexNode indexNode)
    {
        var writer = new PooledMemoryBufferWriter();
        indexNode.WriteTo(writer);
        var key = Guid.NewGuid().ToUInt128Guid();

        var writtenSpan = writer.GetWrittenSpan();

        logger.LogDebug("Flushing index node {Key} with {Count} children of size {Size}", key, indexNode.ChildCount, writtenSpan.Length);


        var id = NextBlockId();
        kvStore.Put(id, writtenSpan);
        return new ReferenceNode(this)
        {
            Id = id,
            Count = indexNode.Count,
            ChildCount = indexNode.ChildCount,
            LastDatom = OnHeapDatom.Create(indexNode.LastDatom)
        };
    }

    public INode Load(StoreKey id)
    {
        if (!kvStore.TryGet(id, out var value))
        {
            throw new InvalidOperationException("Node not found");
        }

        var valueVersion = MemoryMarshal.Read<NodeVersions>(value);
        switch (valueVersion)
        {
            case NodeVersions.DataNode:
            {
                var loaded = new OldAppendableNode(configuration);
                loaded.InitializeFrom(value);
                logger.LogDebug("Loaded data node {Key} with {Count} children of size {Size}", id, loaded.ChildCount, value.Length);
                return loaded;
            }
            case NodeVersions.IndexNode:
            {
                var loaded = new IndexNode(configuration);
                loaded.InitializeFrom(this, value);
                logger.LogDebug("Loaded index node {Key} with {Count} children of size {Size}", id, loaded.ChildCount, value.Length);
                return loaded;
            }
            default:
                throw new InvalidOperationException("Unknown node version " + valueVersion);
        }

    }
}
