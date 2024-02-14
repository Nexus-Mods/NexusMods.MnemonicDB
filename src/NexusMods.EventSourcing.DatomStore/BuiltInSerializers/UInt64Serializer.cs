using System;
using System.Buffers;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore.BuiltInSerializers;

public class UInt64Serializer : IValueSerializer<ulong>
{
    public Type NativeType => typeof(ulong);


    private static readonly UInt128 Id = "876C92DF-9DC2-4B03-879B-C2A14278D3FF".ToUInt128Guid();
    public UInt128 UniqueId => Id;
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(a).CompareTo(BinaryPrimitives.ReadUInt64LittleEndian(b));
    }

    public void Write<TWriter>(ulong value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(8);
        BinaryPrimitives.WriteUInt64LittleEndian(span, value);
        buffer.Advance(8);
    }

    public int Read(ReadOnlySpan<byte> buffer, out ulong val)
    {
        val = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        return 8;
    }
}
