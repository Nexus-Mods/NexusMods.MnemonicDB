
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes;
using NexusMods.EventSourcing.Storage.Columns.BlobColumns;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;
using NexusMods.EventSourcing.Storage.DatomResults;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Nodes.Data;

public partial class DataNode : INode
{
    private readonly bool _isFrozen = true;

    private DataNode(bool isFrozen) : this()
    {
        _isFrozen = isFrozen;
    }

    public static DataNode Create(bool isFrozen = false)
    {
        return new DataNode(isFrozen)
        {
            NumberOfDatoms = 0,
            EntityIds = ULongColumn.Create(),
            AttributeIds = ULongColumn.Create(),
            TransactionIds = ULongColumn.Create(),
            Values = BlobColumn.Create()
        };
    }

    public static DataNode Create(IDatomResult result)
    {
        var newNode = Create();
        newNode.Add(result);
        return newNode;
    }

    public DataNode Freeze()
    {
        if (_isFrozen)
            return this;

        return new DataNode(true)
        {
            NumberOfDatoms = NumberOfDatoms,
            EntityIds = EntityIds.Freeze(),
            AttributeIds = AttributeIds.Freeze(),
            TransactionIds = TransactionIds.Freeze(),
            Values = Values.Freeze()
        };
    }

    public DataNode Thaw()
    {
        if (!_isFrozen)
            return this;

        return new DataNode(false)
        {
            NumberOfDatoms = NumberOfDatoms,
            EntityIds = EntityIds.Thaw(),
            AttributeIds = AttributeIds.Thaw(),
            TransactionIds = TransactionIds.Thaw(),
            Values = Values.Thaw()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureThawed()
    {
        if (_isFrozen)
            throw new InvalidOperationException("Cannot modify a frozen DataNode");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureFrozen()
    {
        if (!_isFrozen)
            throw new InvalidOperationException("Cannot use a thawed DataNode");
    }

    public void Add(ulong entityId, ulong attributeId, ulong transactionId, ReadOnlySpan<byte> value)
    {
        EnsureThawed();
        EntityIds.Add(entityId);
        AttributeIds.Add(attributeId);
        TransactionIds.Add(transactionId);
        Values.Add(value);
        NumberOfDatoms++;
    }

    public void Add(in Datom datom)
    {
        EnsureThawed();
        EntityIds.Add(datom.E.Value);
        AttributeIds.Add(datom.A.Value);
        TransactionIds.Add(datom.T.Value);
        Values.Add(datom.V.Span);
        NumberOfDatoms++;
    }

    public void Add(IEnumerable<Datom> allDatoms)
    {
        EnsureThawed();

        foreach (var datom in allDatoms)
        {
            NumberOfDatoms++;
            EntityIds.Add(datom.E.Value);
            AttributeIds.Add(datom.A.Value);
            TransactionIds.Add(datom.T.Value);
            Values.Add(datom.V.Span);
        }
    }

    public void Add(IDatomResult result)
    {
        // TODO: Use the chunked enumerable to add the datoms
        EnsureThawed();

        using var iterator = result.Iterate();
        while (iterator.Next())
        {
            EntityIds.Add(iterator.Current.EntityIds.CastFast<EntityId, ulong>(), iterator.Current.Mask);
            AttributeIds.Add(iterator.Current.AttributeIds.CastFast<AttributeId, ulong>(), iterator.Current.Mask);
            TransactionIds.Add(iterator.Current.TransactionIds.CastFast<TxId, ulong>(), iterator.Current.Mask);
            Values.Add(iterator.Current);
            NumberOfDatoms += (int)iterator.Current.FilledDatoms;
        }
    }

    public IDatomResult All()
    {
        EnsureFrozen();
        return this;
    }

    public override string ToString()
    {
        return this.DatomResultToString();
    }

    public void Fill(long offset, DatomChunk chunk)
    {
        var size = Math.Min(DatomChunk.ChunkSize, NumberOfDatoms - offset);
        EntityIds.CopyTo((int)offset, chunk.EntityIds.CastFast<EntityId, ulong>().SliceFast(0, (int)size));
        AttributeIds.CopyTo((int)offset, chunk.AttributeIds.CastFast<AttributeId, ulong>().SliceFast(0, (int)size));
        TransactionIds.CopyTo((int)offset, chunk.TransactionIds.CastFast<TxId, ulong>().SliceFast(0, (int)size));

        for (var i = 0; i < size; i++)
        {
            chunk.SetValue(i, Values[(int)(i + offset)]);
        }
        chunk.SetMaskToCount((int)size);
    }

    public void FillValue(long offset, DatomChunk chunk, int idx)
    {
        throw new NotImplementedException();
    }
}
