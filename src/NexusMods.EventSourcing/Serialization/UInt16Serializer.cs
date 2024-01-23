using System;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for unsigned 16 bit integers.
/// </summary>
internal sealed class UInt16Serializer() : AFixedSizeSerializer<ushort>(sizeof(ushort))
{
    /// <inheritdoc />
    public override void Serialize(ushort value, Span<byte> output)
    {
        BinaryPrimitives.WriteUInt16BigEndian(output, value);
    }

    /// <inheritdoc />
    public override ushort Deserialize(ReadOnlySpan<byte> from)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(from);
    }
}
