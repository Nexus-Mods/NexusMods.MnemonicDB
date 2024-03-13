using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.DataNode;
using NexusMods.EventSourcing.Storage.Columns.BlobColumns;
using Reloaded.Memory.Extensions;
using IAppendable = NexusMods.EventSourcing.Abstractions.Nodes.DataNode.IAppendable;

namespace NexusMods.EventSourcing.Storage.Nodes.DataNode;

public class Appendable : AReadable, IAppendable
{
    private Columns.ULongColumns.Appendable _entityIds;
    private Columns.ULongColumns.Appendable _attributeIds;
    private Columns.BlobColumns.Appendable _values;
    private Columns.ULongColumns.Appendable _transactionIds;

    public Appendable(int initialSize = Columns.ULongColumns.Appendable.DefaultSize)
    {
        _length = 0;
        _entityIds = Columns.ULongColumns.Appendable.Create();
        _attributeIds = Columns.ULongColumns.Appendable.Create();
        _values = Columns.BlobColumns.Appendable.Create();
        _transactionIds = Columns.ULongColumns.Appendable.Create();
    }

    public override EntityId GetEntityId(int idx)
    {
        return EntityId.From(_entityIds[idx]);
    }

    public override AttributeId GetAttributeId(int idx)
    {
        return AttributeId.From(_attributeIds[idx]);
    }

    public override TxId GetTransactionId(int idx)
    {
        return TxId.From(_transactionIds[idx]);
    }

    public override ReadOnlySpan<byte> GetValue(int idx)
    {
        return _values[idx];
    }

    public override int FillChunk(int offset, int length, ref DatomChunk chunk)
    {
        throw new NotImplementedException();
    }

    public bool IsFrozen { get; private set; }
    public void Freeze()
    {
        IsFrozen = true;
    }

    private void EnsureNotFrozen()
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException("The node is frozen and cannot be modified.");
        }
    }

    public void Add(Datom datom)
    {
        EnsureNotFrozen();
        _entityIds.Append(datom.E.Value);
        _attributeIds.Append(datom.A.Value);
        _values.Append(datom.V.Span);
        _transactionIds.Append(datom.T.Value);
    }

    public void Add(EntityId entityId, AttributeId attributeId, ReadOnlySpan<byte> value, TxId transactionId)
    {
        EnsureNotFrozen();
        _entityIds.Append(entityId.Value);
        _attributeIds.Append(attributeId.Value);
        _values.Append(value);
        _transactionIds.Append(transactionId.Value);
    }

    public void Add<T>(EntityId entityId, AttributeId attributeId, IValueSerializer<T> serializer, T value, TxId transactionId)
    {
        EnsureNotFrozen();
        _entityIds.Append(entityId.Value);
        _attributeIds.Append(attributeId.Value);
        _values.Append(serializer, value);
        _transactionIds.Append(transactionId.Value);
    }

    public void Add(IReadable other)
    {
        throw new NotImplementedException();
    }

    public void Add(in DatomChunk chunk)
    {
        _entityIds.Append(chunk.EntityIds.CastFast<EntityId, ulong>(), chunk.Mask);
    }

    public IReadable Sort(IDatomComparator comparator)
    {
        throw new NotImplementedException();
    }

    public IAppendable[] Split(int groupCount)
    {
        throw new NotImplementedException();
    }
}
