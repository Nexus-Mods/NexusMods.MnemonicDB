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
            AppendableBlock appendableBlock => Flush(appendableBlock),
            _ => throw new NotImplementedException("Unknown node type. " + node.GetType().Name)
        };
    }

    private ReferenceNode Flush(AppendableBlock appendableBlock)
    {
        var writer = new PooledMemoryBufferWriter();
        appendableBlock.WriteTo(writer);
        var key = Guid.NewGuid().ToUInt128Guid();
        kvStore.Put(key, writer.GetWrittenSpan());
        return new ReferenceNode(this)
        {
            Id = key,
            Count = appendableBlock.Count,
            ChildCount = appendableBlock.Count,
            LastDatom = OnHeapDatom.Create(appendableBlock.LastDatom)
        };
    }

    public INode Load(UInt128 id)
    {
        if (!kvStore.TryGet(id, out var value))
        {
            throw new InvalidOperationException("Node not found");
        }

        var loaded = new AppendableBlock(configuration);
        loaded.InitializeFrom(value);
        return loaded;
    }
}
