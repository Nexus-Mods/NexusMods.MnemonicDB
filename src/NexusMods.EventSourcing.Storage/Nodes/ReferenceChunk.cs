using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.ValueTypes;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class ReferenceChunk(NodeStore store, StoreKey key, IDataChunk? chunk) : IDataChunk
{
    private IDataChunk Resolve()
    {
        return chunk ??= store.Load(key);
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

    public IDataChunk Flush(NodeStore store)
    {
        return this;
    }
}
