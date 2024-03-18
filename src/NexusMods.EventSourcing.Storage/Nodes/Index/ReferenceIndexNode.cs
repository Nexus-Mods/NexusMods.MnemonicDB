using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.Index;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class ReferenceIndexNode : IReadable
{
    private readonly NodeStore _store;
    private readonly StoreKey _key;
    private WeakReference<IReadable>? _node;

    public ReferenceIndexNode(NodeStore store, StoreKey key, WeakReference<IReadable>? node)
    {
        _store = store;
        _key = key;
        _node = node;
    }

    public StoreKey StoreKey => _key;

    public IReadable Resolve()
    {
        if (_node?.TryGetTarget(out var target) == true)
        {
            return target;
        }

        return Load();
    }

    private IReadable Load()
    {
        throw new NotImplementedException();
        /*
        var nodeData = (IReadable)_store.Load(_key);
        _node = new WeakReference<IReadable>(nodeData);
        if (nodeData.Length != Length)
        {
            throw new InvalidOperationException("Node length mismatch");
        }

        return nodeData;
        */
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

    public IEnumerable<IReadable> Children => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public long GetChildCount(int idx)
    {
        throw new NotImplementedException();
    }

    public long GetChildOffset(int idx)
    {
        throw new NotImplementedException();
    }

    public EventSourcing.Abstractions.Nodes.Data.IReadable GetChild(int idx)
    {
        throw new NotImplementedException();
    }

    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable ChildCountsColumn => Resolve().ChildCountsColumn;
    public EventSourcing.Abstractions.Columns.ULongColumns.IReadable ChildOffsetsColumn => Resolve().ChildOffsetsColumn;
    public IDatomComparator Comparator => Resolve().Comparator;
}
