using System;
using System.Buffers.Binary;

namespace NexusMods.EventSourcing.Serialization;

internal class Int16Serializer() : AFixedSizeSerializer<short>(sizeof(short))
{
    public override void Serialize(short value, Span<byte> output)
    {
        BinaryPrimitives.WriteInt16BigEndian(output, value);
    }

    public override short Deserialize(ReadOnlySpan<byte> from)
    {
        return BinaryPrimitives.ReadInt16BigEndian(from);
    }
}
