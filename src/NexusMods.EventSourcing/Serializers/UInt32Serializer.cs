using System;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serializers;

/// <inheritdoc />
public sealed class UInt32Serializer : IStaticValueSerializer<uint>
{
    /// <inheritdoc />
    public Type ForType => typeof(uint);

    /// <inheritdoc />
    public bool IsDynamicSize => false;

    /// <inheritdoc />
    public void Deserialize(ref Span<byte> span, ref uint value)
    {
        value = BinaryPrimitives.ReadUInt32BigEndian(span);
    }

    /// <inheritdoc />
    public void Serialize(ref Span<byte> span, ref uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(span, value);
    }

    /// <inheritdoc />
    public int GetSize()
    {
        return 4;
    }
}
