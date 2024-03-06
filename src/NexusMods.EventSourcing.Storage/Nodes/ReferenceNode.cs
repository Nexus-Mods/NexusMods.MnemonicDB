using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class ReferenceNode(NodeStore store, StoreKey key, WeakReference<IDataNode>? chunk) : IDataNode
{
    private IDataNode Resolve()
    {
        if (chunk?.TryGetTarget(out var target) == true)
        {
            return target;
        }
        return LoadNode();
    }

    private IDataNode LoadNode()
    {
        var chunkData = store.Load(key);
        chunk = new WeakReference<IDataNode>(chunkData);
        if (chunkData.Length != Length)
        {
            throw new InvalidOperationException("Node length mismatch");
        }

        return chunkData;
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

    public bool IsResolved => chunk != null;
    public int Length => Resolve().Length;
    public IColumn<EntityId> EntityIds => Resolve().EntityIds;
    public IColumn<AttributeId> AttributeIds => Resolve().AttributeIds;
    public IColumn<TxId> TransactionIds => Resolve().TransactionIds;
    public IColumn<DatomFlags> Flags => Resolve().Flags;
    public IBlobColumn Values => Resolve().Values;

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
}
