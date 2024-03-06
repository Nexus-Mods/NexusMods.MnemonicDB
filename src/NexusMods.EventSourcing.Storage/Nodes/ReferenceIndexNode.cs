using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class ReferenceIndexNode(NodeStore store, StoreKey key, IIndexNode? chunk) : AIndexNode
{
    public IIndexNode Resolve()
    {
        return chunk ??= (IIndexNode)store.Load(key);
    }
    public override IEnumerator<Datom> GetEnumerator()
    {
        return Resolve().GetEnumerator();
    }

    public override int Length => Resolve().Length;
    public override IColumn<EntityId> EntityIds => Resolve().EntityIds;
    public override IColumn<AttributeId> AttributeIds => Resolve().AttributeIds;
    public override IColumn<TxId> TransactionIds => Resolve().TransactionIds;
    public override IColumn<DatomFlags> Flags => Resolve().Flags;
    public override IBlobColumn Values => Resolve().Values;

    public override Datom this[int idx] => Resolve()[idx];

    public override Datom LastDatom => Resolve().LastDatom;
    public override void WriteTo<TWriter>(TWriter writer)
    {
        throw new NotSupportedException();
    }

    public override IDataNode Flush(INodeStore store)
    {
        throw new NotSupportedException();
    }

    public override IEnumerable<IDataNode> Children => Resolve().Children;
    public override IColumn<int> ChildCounts => Resolve().ChildCounts;
    public override IColumn<int> ChildOffsets => Resolve().ChildOffsets;
    public override IDatomComparator Comparator => Resolve().Comparator;

    public override IDataNode ChildAt(int idx)
    {
        return Resolve().ChildAt(idx);
    }
}
