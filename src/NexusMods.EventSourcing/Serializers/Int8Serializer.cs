using System;
using System.Buffers.Binary;
using System.Linq.Expressions;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serializers;

/// <summary>
/// Int8Serializer
/// </summary>
public sealed class Int8Serializer : IStaticValueSerializer<byte>
{
    /// <inheritdoc />
    public Type ForType => typeof(byte);

    /// <inheritdoc />
    public bool IsDynamicSize => false;

    /// <inheritdoc />
    public void Deserialize(ref Span<byte> span, ref byte value)
    {
        value = span[0];
    }

    /// <inheritdoc />
    public void Serialize(ref Span<byte> span, ref byte value)
    {
        span[0] = value;
    }

    /// <inheritdoc />
    public int GetSize()
    {
        return sizeof(byte);
    }
}



