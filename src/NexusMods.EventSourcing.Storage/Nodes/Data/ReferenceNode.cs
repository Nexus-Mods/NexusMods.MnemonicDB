using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Nodes.Data;

public class ReferenceNode(NodeStore store, StoreKey key, WeakReference<IReadable>? node) : IReadable
{
    private IReadable Resolve()
    {
        if (node?.TryGetTarget(out var target) == true)
        {
            return target;
        }
        return LoadNode();
    }

    private IReadable LoadNode()
    {
        var nodeData = store.Load(key);
        node = new WeakReference<IReadable>(nodeData);
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

    public long DeepLength => Resolve().DeepLength;
    public int Length => Resolve().Length;

    public Datom this[int idx] => Resolve()[idx];
    public Datom LastDatom => Resolve().LastDatom;
    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new NotSupportedException();
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

    public int FillChunk(int offset, int length, ref DatomChunk chunk)
    {
        throw new NotImplementedException();
    }

    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable EntityIdsColumn => Resolve().EntityIdsColumn;
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable AttributeIdsColumn => Resolve().AttributeIdsColumn;
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable TransactionIdsColumn => Resolve().TransactionIdsColumn;
    public EventSourcing.Abstractions.Columns.BlobColumns.IReadable ValuesColumn => Resolve().ValuesColumn;
}
