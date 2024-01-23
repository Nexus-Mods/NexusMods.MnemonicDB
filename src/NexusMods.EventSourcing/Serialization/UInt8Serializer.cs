using System;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for writing UInt8.
/// </summary>
internal sealed class UInt8Serializer() : AFixedSizeSerializer<byte>(sizeof(byte))
{
    /// <inheritdoc />
    public override void Serialize(byte value, Span<byte> output)
    {
        output[0] = value;
    }

    /// <inheritdoc />
    public override byte Deserialize(ReadOnlySpan<byte> from)
    {
        return from[0];
    }
}
