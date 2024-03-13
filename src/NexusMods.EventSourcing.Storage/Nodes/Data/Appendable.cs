using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.Data;
using Reloaded.Memory.Extensions;
using Data_IAppendable = NexusMods.EventSourcing.Abstractions.Nodes.Data.IAppendable;
using IAppendable = NexusMods.EventSourcing.Abstractions.Nodes.Data.IAppendable;
using IPacked = NexusMods.EventSourcing.Abstractions.Nodes.Data.IPacked;

namespace NexusMods.EventSourcing.Storage.Nodes.Data;

public class Appendable : Data_IAppendable, IReadable
{
    private Columns.ULongColumns.Appendable _entityIds;
    private Columns.ULongColumns.Appendable _attributeIds;
    private Columns.BlobColumns.Appendable _values;
    private Columns.ULongColumns.Appendable _transactionIds;
    private int _length;

    public Appendable(int initialSize = Columns.ULongColumns.Appendable.DefaultSize)
    {
        _length = 0;
        _entityIds = Columns.ULongColumns.Appendable.Create();
        _attributeIds = Columns.ULongColumns.Appendable.Create();
        _values = Columns.BlobColumns.Appendable.Create();
        _transactionIds = Columns.ULongColumns.Appendable.Create();
    }

    #region Public Constructors

    /// <summary>
    /// Creates a new <see cref="Appendable"/> via cloning the given <see cref="IReadable"/>.
    /// </summary>
    public static Appendable Create(IReadable readable)
    {
        var appendable = new Appendable(readable.Length);
        appendable.Add(readable);
        return appendable;
    }


    #endregion

    /// <inheritdoc />
    public int Length => _length;

    /// <inheritdoc />
    public long DeepLength => _length;

    /// <inheritdoc />
    public Datom this[int idx] => new()
    {
        E = EntityId.From(_entityIds[idx]),
        A = AttributeId.From(_attributeIds[idx]),
        T = TxId.From(_transactionIds[idx]),
        V = _values.GetMemory(idx)
    };

    public Datom LastDatom { get; }

    public EntityId GetEntityId(int idx)
    {
        return EntityId.From(_entityIds[idx]);
    }

    public AttributeId GetAttributeId(int idx)
    {
        return AttributeId.From(_attributeIds[idx]);
    }

    public TxId GetTransactionId(int idx)
    {
        return TxId.From(_transactionIds[idx]);
    }

    public ReadOnlySpan<byte> GetValue(int idx)
    {
        return _values[idx];
    }

    public int FillChunk(int offset, int length, ref DatomChunk chunk)
    {
        throw new NotImplementedException();
    }

    public bool IsFrozen { get; private set; }
    public void Freeze()
    {
        IsFrozen = true;
    }

    public IPacked Pack()
    {
        /*
        return new DataPackedNode
        {
            Length = _length,
            EntityIds = (ULongPackedColumn)_entityIds.Pack(),
            AttributeIds = (ULongPackedColumn)_attributeIds.Pack(),
            Values = (BlobPackedColumn)_values.Pack(),
            TransactionIds = (ULongPackedColumn)_transactionIds.Pack()
        };*/
        throw new NotImplementedException();
    }

    private void EnsureNotFrozen()
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException("The node is frozen and cannot be modified.");
        }
    }

    public void Add(in Datom datom)
    {
        EnsureNotFrozen();
        _length++;
        _entityIds.Append(datom.E.Value);
        _attributeIds.Append(datom.A.Value);
        _values.Append(datom.V.Span);
        _transactionIds.Append(datom.T.Value);
    }

    public void Add(EntityId entityId, AttributeId attributeId, ReadOnlySpan<byte> value, TxId transactionId)
    {
        EnsureNotFrozen();
        _length++;
        _entityIds.Append(entityId.Value);
        _attributeIds.Append(attributeId.Value);
        _values.Append(value);
        _transactionIds.Append(transactionId.Value);
    }

    public void Add<T>(EntityId entityId, AttributeId attributeId, IValueSerializer<T> serializer, T value, TxId transactionId)
    {
        EnsureNotFrozen();
        _length++;
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

    public IEnumerator<Datom> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
