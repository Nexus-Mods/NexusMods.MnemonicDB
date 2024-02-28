using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Algorithms;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class PackedChunk : IDataChunk
{
    public PackedChunk(int length, IColumn<EntityId> entityIds, IColumn<AttributeId> attributeIds, IColumn<TxId> transactionIds, IColumn<DatomFlags> flags, IBlobColumn values)
    {
        EntityIds = entityIds;
        AttributeIds = attributeIds;
        TransactionIds = transactionIds;
        Flags = flags;
        Values = values;
        Length = length;
    }

    public int Length { get; }
    public IColumn<EntityId> EntityIds { get; }
    public IColumn<AttributeId> AttributeIds { get; }
    public IColumn<TxId> TransactionIds { get; }
    public IColumn<DatomFlags> Flags { get; }
    public IBlobColumn Values { get; }

    public Datom this[int idx] => new()
    {
        E = EntityIds[idx],
        A = AttributeIds[idx],
        T = TransactionIds[idx],
        F = Flags[idx],
        V = Values[idx]
    };

    public Datom LastDatom => this[Length - 1];

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        writer.WriteFourCC(FourCC.PackedData);
        writer.Write(Length);

        EntityIds.WriteTo(writer);
        AttributeIds.WriteTo(writer);
        TransactionIds.WriteTo(writer);
        Flags.WriteTo(writer);
        Values.WriteTo(writer);
    }

    public IDataChunk Flush(NodeStore store)
    {
        return store.Flush(this);
    }

    public static PackedChunk ReadFrom(ref BufferReader src)
    {
        var length = src.Read<uint>();
        var entityIds = ColumnReader.ReadColumn<EntityId>(ref src, (int)length);
        var attributeIds = ColumnReader.ReadColumn<AttributeId>(ref src, (int)length);
        var transactionIds = ColumnReader.ReadColumn<TxId>(ref src, (int)length);
        var flags = ColumnReader.ReadColumn<DatomFlags>(ref src, (int)length);
        var values = ColumnReader.ReadBlobColumn(ref src, (int)length);

        return new PackedChunk((int)length, entityIds, attributeIds, transactionIds, flags, values);
    }

    public IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < Length; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
