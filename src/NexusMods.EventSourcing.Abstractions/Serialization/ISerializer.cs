using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
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
    public T Deserialize(ReadOnlySpan<byte> from);
}

public interface IVariableSizeSerializer<T> : ISerializer
{
    public void Serialize<TWriter>(T value, TWriter output) where TWriter : IBufferWriter<byte>;
    public int Deserialize(ReadOnlySpan<byte> from, out T value);
}


/// <summary>
/// If the serializer can specialize (e.g. for a generic type), it should implement this interface.
/// </summary>
public interface IGenericSerializer : ISerializer
{
    public bool TrySpecialze(Type baseType, Type[] argTypes, Func<Type, ISerializer> serializerFinder, [NotNullWhen(true)] out ISerializer? serializer);
}
