using System;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

public sealed class GuidSerializer : IFixedSizeSerializer<Guid>
{
    public bool CanSerialize(Type valueType)
    {
        return valueType == typeof(Guid);
    }

    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 16;
        return true;
    }

    public void Serialize(Guid value, Span<byte> output)
    {
        value.TryWriteBytes(output);
    }

    public Guid Deserialize(ReadOnlySpan<byte> from)
    {
        return new(from);
    }
}
