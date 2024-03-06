using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Algorithms;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class PackedNode : ADataNode
{
    public PackedNode(int length, IColumn<EntityId> entityIds, IColumn<AttributeId> attributeIds, IColumn<TxId> transactionIds, IColumn<DatomFlags> flags, IBlobColumn values)
    {
        EntityIds = entityIds;
        AttributeIds = attributeIds;
        TransactionIds = transactionIds;
        Flags = flags;
        Values = values;
        Length = length;
    }

    public override int Length { get; }
    public override IColumn<EntityId> EntityIds { get; }
    public override IColumn<AttributeId> AttributeIds { get; }
    public override IColumn<TxId> TransactionIds { get; }
    public override IColumn<DatomFlags> Flags { get; }
    public override IBlobColumn Values { get; }

    public override Datom this[int idx] => new()
    {
        E = EntityIds[idx],
        A = AttributeIds[idx],
        T = TransactionIds[idx],
        F = Flags[idx],
        V = Values[idx]
    };

    public override Datom LastDatom => this[Length - 1];

    public override void WriteTo<TWriter>(TWriter writer)
    {
        writer.WriteFourCC(FourCC.PackedData);
        writer.Write(Length);

        EntityIds.WriteTo(writer);
        AttributeIds.WriteTo(writer);
        TransactionIds.WriteTo(writer);
        Flags.WriteTo(writer);
        Values.WriteTo(writer);
    }

    public override IDataNode Flush(INodeStore store)
    {
        return store.Flush(this);
    }

    public static PackedNode ReadFrom(ref BufferReader src)
    {
        var length = src.Read<uint>();
        var entityIds = ColumnReader.ReadColumn<EntityId>(ref src, (int)length);
        var attributeIds = ColumnReader.ReadColumn<AttributeId>(ref src, (int)length);
        var transactionIds = ColumnReader.ReadColumn<TxId>(ref src, (int)length);
        var flags = ColumnReader.ReadColumn<DatomFlags>(ref src, (int)length);
        var values = ColumnReader.ReadBlobColumn(ref src, (int)length);

        return new PackedNode((int)length, entityIds, attributeIds, transactionIds, flags, values);
    }

    public override IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < Length; i++)
        {
            yield return this[i];
        }
    }
}
