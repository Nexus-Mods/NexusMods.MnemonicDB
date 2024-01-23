using System;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// A abstract fixed size serializer.
/// </summary>
/// <param name="size"></param>
/// <typeparam name="TType"></typeparam>
public abstract class AFixedSizeSerializer<TType>(int Size) : IFixedSizeSerializer<TType>
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType)
    {
        return valueType == typeof(TType);
    }

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc />
    public abstract void Serialize(TType value, Span<byte> output);

    /// <inheritdoc />
    public abstract TType Deserialize(ReadOnlySpan<byte> from);
}
