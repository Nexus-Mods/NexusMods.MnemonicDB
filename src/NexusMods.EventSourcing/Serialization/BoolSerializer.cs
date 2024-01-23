using System;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for bools.
/// </summary>
internal sealed class BoolSerializer() : AFixedSizeSerializer<bool>(sizeof(bool))
{
    /// <inheritdoc />
    public override void Serialize(bool value, Span<byte> output)
    {
        output[0] = value ? (byte)1 : (byte)0;
    }

    /// <inheritdoc />
    public override bool Deserialize(ReadOnlySpan<byte> from)
    {
        return from[0] == 1;
    }
}
