using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class ReferenceIndexNode : IIndexNode
{
    private readonly NodeStore _store;
    private readonly StoreKey _key;
    private WeakReference<IIndexNode>? _node;

    public ReferenceIndexNode(NodeStore store, StoreKey key, WeakReference<IIndexNode>? node)
    {
        _store = store;
        _key = key;
        _node = node;
    }

    public StoreKey Key => _key;

    public IIndexNode Resolve()
    {
        if (_node?.TryGetTarget(out var target) == true)
        {
            return target;
        }

        return Load();
    }

    private IIndexNode Load()
    {
        var nodeData = (IIndexNode)_store.Load(_key);
        _node = new WeakReference<IIndexNode>(nodeData);
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

    public long DeepLength => Resolve().DeepLength;
    public int Length => Resolve().Length;
    public  Datom this[int idx] => Resolve()[idx];

    public Datom LastDatom => Resolve().LastDatom;
    public void WriteTo<TWriter>(TWriter writer)
    {
        throw new NotSupportedException();
    }

    void IDataNode.WriteTo<TWriter>(TWriter writer)
    {
        WriteTo(writer);
    }

    public IDataNode Flush(INodeStore store)
    {
        throw new NotSupportedException();
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

    public IEnumerable<IDataNode> Children => Resolve().Children;
    public IColumn<int> ChildCounts => Resolve().ChildCounts;
    public IColumn<int> ChildOffsets => Resolve().ChildOffsets;
    public IDatomComparator Comparator => Resolve().Comparator;

    public IDataNode ChildAt(int idx)
    {
        return Resolve().ChildAt(idx);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
