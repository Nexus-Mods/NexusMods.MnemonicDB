using System;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

public sealed class UInt8Serializer : IFixedSizeSerializer<byte>
{
    public bool CanSerialize(Type valueType) => valueType == typeof(byte);

    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = sizeof(byte);
        return valueType == typeof(byte);
    }

    public void Serialize(byte value, Span<byte> output)
    {
        output[0] = value;
    }

    public byte Deserialize(ReadOnlySpan<byte> from)
    {
        return from[0];
    }
}
