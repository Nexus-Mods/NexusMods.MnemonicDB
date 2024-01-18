using System;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

public sealed class UInt64Serializer : IFixedSizeSerializer<ulong>
{
    public bool CanSerialize(Type valueType) => valueType == typeof(ulong);

    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = sizeof(ulong);
        return valueType == typeof(ulong);
    }

    public void Serialize(ulong value, Span<byte> output)
    {
        BinaryPrimitives.WriteUInt64BigEndian(output, value);
    }

    public ulong Deserialize(ReadOnlySpan<byte> from)
    {
        return BinaryPrimitives.ReadUInt64BigEndian(from);
    }
}
