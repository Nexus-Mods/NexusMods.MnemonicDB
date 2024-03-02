using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class ReferenceIndexChunk(NodeStore store, StoreKey key, IIndexChunk? chunk) : IIndexChunk
{
    public IIndexChunk Resolve()
    {
        return chunk ??= (IIndexChunk)store.Load(key);
    }
    public IEnumerator<Datom> GetEnumerator()
    {
        return Resolve().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

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

    public IDataChunk Flush(INodeStore store)
    {
        throw new NotSupportedException();
    }

    public IEnumerable<IDataChunk> Children => Resolve().Children;
    public IColumn<int> ChildCounts => Resolve().ChildCounts;
    public IDatomComparator Comparator => Resolve().Comparator;
}
