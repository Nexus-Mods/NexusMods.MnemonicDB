using System;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for floats.
/// </summary>
internal sealed class FloatSerializer() : AFixedSizeSerializer<float>(sizeof(float))
{
    /// <inheritdoc />
    public override void Serialize(float value, Span<byte> output)
    {
        BinaryPrimitives.WriteSingleBigEndian(output, value);
    }

    /// <inheritdoc />
    public override float Deserialize(ReadOnlySpan<byte> from)
    {
        return BinaryPrimitives.ReadSingleBigEndian(from);
    }
}
