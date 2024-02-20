using System;
using System.Buffers;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions;
namespace NexusMods.EventSourcing.DatomStore.BuiltInSerializers;

public class TxIdSerializer : IValueSerializer<TxId>
{
    public Type NativeType => typeof(TxId);

    public static readonly UInt128 Id = "BB2B2BAF-9AA8-4DB0-8BFC-A0A853ED9BA0".ToUInt128Guid();
    public UInt128 UniqueId => Id;
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(a).CompareTo(BinaryPrimitives.ReadUInt64LittleEndian(b));
    }

    public void Write<TWriter>(TxId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(8);
        BinaryPrimitives.WriteUInt64LittleEndian(span, value.Value);
        buffer.Advance(8);
    }

    public int Read(ReadOnlySpan<byte> buffer, out TxId val)
    {
        val = TxId.From(BinaryPrimitives.ReadUInt64LittleEndian(buffer));
        return 8;
    }
}
