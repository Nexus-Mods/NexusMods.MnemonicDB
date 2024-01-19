using System;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for bools.
/// </summary>
public class BoolSerializer : IFixedSizeSerializer<bool>
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType)
    {
        return valueType == typeof(bool);
    }

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = sizeof(bool);
        return valueType == typeof(bool);
    }

    /// <inheritdoc />
    public void Serialize(bool value, Span<byte> output)
    {
        output[0] = value ? (byte)1 : (byte)0;
    }

    /// <inheritdoc />
    public bool Deserialize(ReadOnlySpan<byte> from)
    {
        return from[0] == 1;
    }
}
