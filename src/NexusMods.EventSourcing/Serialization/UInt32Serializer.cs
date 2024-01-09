using System;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

public class UInt32Serializer : IFixedSizeSerializer<uint>
{
    public bool CanSerialize(Type valueType) => valueType == typeof(uint);

    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = sizeof(uint);
        return valueType == typeof(uint);
    }

    public void Serialize(uint value, Span<byte> output)
    {
        BinaryPrimitives.WriteUInt32BigEndian(output, value);
    }

    public uint Deserialize(Span<byte> from)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(from);
    }
}
