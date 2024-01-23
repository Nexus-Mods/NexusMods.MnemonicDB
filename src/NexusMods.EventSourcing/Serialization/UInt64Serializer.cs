using System;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for writing UInt64.
/// </summary>
internal sealed class UInt64Serializer() : AFixedSizeSerializer<ulong>(sizeof(ulong))
{
    /// <inheritdoc />
    public override void Serialize(ulong value, Span<byte> output)
    {
        BinaryPrimitives.WriteUInt64BigEndian(output, value);
    }

    /// <inheritdoc />
    public override ulong Deserialize(ReadOnlySpan<byte> from)
    {
        return BinaryPrimitives.ReadUInt64BigEndian(from);
    }
}
