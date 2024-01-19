using System;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for writing UInt8.
/// </summary>
public sealed class UInt8Serializer : IFixedSizeSerializer<byte>
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType) => valueType == typeof(byte);

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = sizeof(byte);
        return valueType == typeof(byte);
    }

    /// <inheritdoc />
    public void Serialize(byte value, Span<byte> output)
    {
        output[0] = value;
    }

    /// <inheritdoc />
    public byte Deserialize(ReadOnlySpan<byte> from)
    {
        return from[0];
    }
}
