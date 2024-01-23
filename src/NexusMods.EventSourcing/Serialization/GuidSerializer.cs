using System;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for Guids.
/// </summary>
internal sealed class GuidSerializer() : AFixedSizeSerializer<Guid>(16)
{
    /// <inheritdoc />
    public override void Serialize(Guid value, Span<byte> output)
    {
        value.TryWriteBytes(output);
    }

    /// <inheritdoc />
    public override Guid Deserialize(ReadOnlySpan<byte> from)
    {
        return new(from);
    }
}
