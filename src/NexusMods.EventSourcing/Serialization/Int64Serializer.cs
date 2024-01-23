using System;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

internal sealed class Int64Serializer() : AFixedSizeSerializer<long>(sizeof(long))
{
    public override void Serialize(long value, Span<byte> output)
    {
        BinaryPrimitives.WriteInt64BigEndian(output, value);
    }

    public override long Deserialize(ReadOnlySpan<byte> from)
    {
        return (long) BinaryPrimitives.ReadInt64BigEndian(from);
    }
}
