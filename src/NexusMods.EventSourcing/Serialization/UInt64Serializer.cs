using System;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for writing UInt64.
/// </summary>
public sealed class UInt64Serializer : IFixedSizeSerializer<ulong>
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType) => valueType == typeof(ulong);

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = sizeof(ulong);
        return valueType == typeof(ulong);
    }

    /// <inheritdoc />
    public void Serialize(ulong value, Span<byte> output)
    {
        BinaryPrimitives.WriteUInt64BigEndian(output, value);
    }

    /// <inheritdoc />
    public ulong Deserialize(ReadOnlySpan<byte> from)
    {
        return BinaryPrimitives.ReadUInt64BigEndian(from);
    }
}
