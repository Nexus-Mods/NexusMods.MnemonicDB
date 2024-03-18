
using System;
using System.Runtime.CompilerServices;
using NexusMods.EventSourcing.Storage.Columns.BlobColumns;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;

namespace NexusMods.EventSourcing.Storage.Nodes.Data;

public partial class DataNode
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
            Length = 0,
            EntityIds = ULongColumn.Create(),
            AttributeIds = ULongColumn.Create(),
            TransactionIds = ULongColumn.Create(),
            Values = BlobColumn.Create()
        };
    }

    public DataNode Freeze()
    {
        if (_isFrozen)
            return this;

        return new DataNode(true)
        {
            Length = Length,
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
            Length = Length,
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

    public void Add(ulong entityId, ulong attributeId, ulong transactionId, ReadOnlySpan<byte> value)
    {
        EnsureThawed();
        EntityIds.Add(entityId);
        AttributeIds.Add(attributeId);
        TransactionIds.Add(transactionId);
        Values.Add(value);
    }

}
