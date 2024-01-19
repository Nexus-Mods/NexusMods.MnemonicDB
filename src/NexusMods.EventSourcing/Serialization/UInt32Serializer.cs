using System;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for writing UInt32.
/// </summary>
public sealed class UInt32Serializer : IFixedSizeSerializer<uint>
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType) => valueType == typeof(uint);

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = sizeof(uint);
        return valueType == typeof(uint);
    }

    /// <inheritdoc />
    public void Serialize(uint value, Span<byte> output)
    {
        BinaryPrimitives.WriteUInt32BigEndian(output, value);
    }

    /// <inheritdoc />
    public uint Deserialize(ReadOnlySpan<byte> from)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(from);
    }
}
