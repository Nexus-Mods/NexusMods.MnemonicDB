using System;
using System.Buffers.Binary;

namespace NexusMods.EventSourcing.Serialization;

internal sealed class DoubleSerializer() : AFixedSizeSerializer<double>(sizeof(double))
{
    public override void Serialize(double value, Span<byte> output)
    {
        BinaryPrimitives.WriteDoubleBigEndian(output, value);
    }

    public override double Deserialize(ReadOnlySpan<byte> from)
    {
        return BinaryPrimitives.ReadDoubleBigEndian(from);
    }
}
