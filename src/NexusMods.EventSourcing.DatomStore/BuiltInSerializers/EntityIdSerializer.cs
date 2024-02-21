using System;
using System.Buffers;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore.BuiltInSerializers;

public class EntityIdSerialzer : IValueSerializer<EntityId>
{
    public Type NativeType => typeof(EntityId);

    private static readonly UInt128 Id = "E2C3185E-C082-4641-B25E-7CEC803A2F48".ToUInt128Guid();
    public UInt128 UniqueId => Id;
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(a).CompareTo(BinaryPrimitives.ReadUInt64LittleEndian(b));
    }

    public void Write<TWriter>(EntityId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(8);
        BinaryPrimitives.WriteUInt64LittleEndian(span, value.Value);
        buffer.Advance(8);
    }

    public int Read(ReadOnlySpan<byte> buffer, out EntityId val)
    {
        val = EntityId.From(BinaryPrimitives.ReadUInt64LittleEndian(buffer));
        return 8;
    }
}
