using System;
using System.Buffers.Binary;

namespace NexusMods.EventSourcing.Serialization;

internal sealed class Int32Serializer() : AFixedSizeSerializer<int>(sizeof(int)) {
    public override void Serialize(int value, Span<byte> output)
    {
        BinaryPrimitives.WriteInt32BigEndian(output, value);
    }

    public override int Deserialize(ReadOnlySpan<byte> from)
    {
        return BinaryPrimitives.ReadInt32BigEndian(from);
    }
}
