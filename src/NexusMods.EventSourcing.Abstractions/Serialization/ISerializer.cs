using System;
using System.IO;

namespace NexusMods.EventSourcing.Abstractions.Serialization;

public interface ISerializer
{
    public bool CanSerialize(Type valueType);
    public bool TryGetFixedSize(Type valueType, out int size);
}


public interface IFixedSizeSerializer<T> : ISerializer
{
    public void Serialize(T value, Span<byte> output);
    public T Deserialize(Span<byte> from);
}
