using System;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for Guids.
/// </summary>
public sealed class GuidSerializer : IFixedSizeSerializer<Guid>
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType)
    {
        return valueType == typeof(Guid);
    }

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 16;
        return true;
    }

    /// <inheritdoc />
    public void Serialize(Guid value, Span<byte> output)
    {
        value.TryWriteBytes(output);
    }

    /// <inheritdoc />
    public Guid Deserialize(ReadOnlySpan<byte> from)
    {
        return new(from);
    }
}
