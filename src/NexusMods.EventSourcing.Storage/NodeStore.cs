using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Nodes;

namespace NexusMods.EventSourcing.Storage;

public class NodeStore(IKvStore kvStore, Configuration configuration)
{

    public ReferenceNode Flush(INode node)
    {
        return node switch
        {
            ReferenceNode referenceNode => referenceNode,
            AppendableNode appendableBlock => Flush(appendableBlock),
            IndexNode indexNode => Flush(indexNode),
            _ => throw new NotImplementedException("Unknown node type. " + node.GetType().Name)
        };
    }

    private ReferenceNode Flush(AppendableNode appendableNode)
    {
        var writer = new PooledMemoryBufferWriter();
        appendableNode.WriteTo(writer);
        var key = Guid.NewGuid().ToUInt128Guid();
        kvStore.Put(key, writer.GetWrittenSpan());
        return new ReferenceNode(this)
        {
            Id = key,
            Count = appendableNode.Count,
            ChildCount = appendableNode.Count,
            LastDatom = OnHeapDatom.Create(appendableNode.LastDatom)
        };
    }

    private ReferenceNode Flush(IndexNode indexNode)
    {
        var writer = new PooledMemoryBufferWriter();
        indexNode.WriteTo(writer);
        var key = Guid.NewGuid().ToUInt128Guid();
        kvStore.Put(key, writer.GetWrittenSpan());
        return new ReferenceNode(this)
        {
            Id = key,
            Count = indexNode.Count,
            ChildCount = indexNode.ChildCount,
            LastDatom = OnHeapDatom.Create(indexNode.LastDatom)
        };
    }

    public INode Load(UInt128 id)
    {
        if (!kvStore.TryGet(id, out var value))
        {
            throw new InvalidOperationException("Node not found");
        }

        var loaded = new AppendableNode(configuration);
        loaded.InitializeFrom(value);
        return loaded;
    }
}
