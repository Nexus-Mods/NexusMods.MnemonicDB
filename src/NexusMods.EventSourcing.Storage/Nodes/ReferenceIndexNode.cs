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
    private IIndexNode? _chunk;

    public ReferenceIndexNode(NodeStore store, StoreKey key, IIndexNode? chunk)
    {
        _store = store;
        _key = key;
        _chunk = chunk;
    }

    public IIndexNode Resolve()
    {
        return _chunk ??= (IIndexNode)_store.Load(_key);
    }
    public IEnumerator<Datom> GetEnumerator()
    {
        return Resolve().GetEnumerator();
    }

    public int Length => Resolve().Length;
    public IColumn<EntityId> EntityIds => Resolve().EntityIds;
    public IColumn<AttributeId> AttributeIds => Resolve().AttributeIds;
    public IColumn<TxId> TransactionIds => Resolve().TransactionIds;
    public IColumn<DatomFlags> Flags => Resolve().Flags;
    public IBlobColumn Values => Resolve().Values;

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
