using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes.DataNode;

public class ReferenceNode(NodeStore store, StoreKey key, WeakReference<IDataNode>? node) : IDataNode
{
    private IDataNode Resolve()
    {
        if (node?.TryGetTarget(out var target) == true)
        {
            return target;
        }
        return LoadNode();
    }

    private IDataNode LoadNode()
    {
        var nodeData = store.Load(key);
        node = new WeakReference<IDataNode>(nodeData);
        if (nodeData.Length != Length)
        {
            throw new InvalidOperationException("Node length mismatch");
        }

        return nodeData;
    }

    public IEnumerator<Datom> GetEnumerator()
    {
        return Resolve().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public StoreKey Key => key;

    public bool IsResolved => node != null;
    public long DeepLength { get; }
    public int Length => Resolve().Length;

    public Datom this[int idx] => Resolve()[idx];
    public Datom LastDatom => Resolve().LastDatom;
    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new NotSupportedException();
    }

    public IDataNode Flush(INodeStore store)
    {
        return this;
    }

    public int FindEATV(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        return Resolve().FindEATV(start, end, target, registry);
    }

    public int FindAVTE(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        return Resolve().FindAVTE(start, end, target, registry);
    }

    public int FindAETV(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        return Resolve().FindAETV(start, end, target, registry);
    }

    public int Find(int start, int end, in Datom target, SortOrders order, IAttributeRegistry registry)
    {
        return Resolve().Find(start, end, target, order, registry);
    }

    public EntityId GetEntityId(int idx)
    {
        return Resolve().GetEntityId(idx);
    }

    public AttributeId GetAttributeId(int idx)
    {
        return Resolve().GetAttributeId(idx);
    }

    public TxId GetTransactionId(int idx)
    {
        return Resolve().GetTransactionId(idx);
    }

    public ReadOnlySpan<byte> GetValue(int idx)
    {
        return Resolve().GetValue(idx);
    }
}
