using System;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for writing UInt32.
/// </summary>
internal sealed class UInt32Serializer() : AFixedSizeSerializer<uint>(sizeof(uint))
{
    /// <inheritdoc />
    public override void Serialize(uint value, Span<byte> output)
    {
        BinaryPrimitives.WriteUInt32BigEndian(output, value);
    }

    /// <inheritdoc />
    public override uint Deserialize(ReadOnlySpan<byte> from)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(from);
    }
}
