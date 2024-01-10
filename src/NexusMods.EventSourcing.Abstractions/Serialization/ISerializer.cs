using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace NexusMods.EventSourcing.Abstractions.Serialization;

/// <summary>
/// Base interface for all serializers, both fixed and variable size.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Returns true if the given type can be serialized by this serializer.
    /// </summary>
    /// <param name="valueType"></param>
    /// <returns></returns>
    public bool CanSerialize(Type valueType);

    /// <summary>
    /// If the serializer is a fixed size serializer, returns true and the size of the serialized value.
    /// </summary>
    /// <param name="valueType"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public bool TryGetFixedSize(Type valueType, out int size);
}

/// <summary>
/// Interface for fixed size serializers, these are serializers that know their size at compile time, for example
/// ints, and guids, but not strings.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IFixedSizeSerializer<T> : ISerializer
{
    /// <summary>
    /// Serialize the given value into the given span. The span will be exactly the size of the value returned
    /// by <see cref="TryGetFixedSize"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="output"></param>
    public void Serialize(T value, Span<byte> output);

    /// <summary>
    /// Deserialize the given span into the given value. The span will be exactly the size of the value returned
    /// </summary>
    /// <param name="from"></param>
    /// <returns></returns>
    public T Deserialize(ReadOnlySpan<byte> from);
}

/// <summary>
/// A variable size serializer is one that does not know the size of the serialized value at compile time, for example
/// strings, arrays, and other collections.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IVariableSizeSerializer<T> : ISerializer
{
    /// <summary>
    /// Serialize the given value into the given buffer writer.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="output"></param>
    /// <typeparam name="TWriter"></typeparam>
    public void Serialize<TWriter>(T value, TWriter output) where TWriter : IBufferWriter<byte>;

    /// <summary>
    /// Deserialize the given span into the given value, and return the number of bytes read. The span
    /// will be at least the size of bytes written by <see cref="Serialize{TWriter}"/>. But will often
    /// be larger (and that data will contain the values of the next serialized value).
    /// </summary>
    /// <param name="from"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public int Deserialize(ReadOnlySpan<byte> from, out T value);
}


/// <summary>
/// If the serializer can specialize (e.g. for a generic type), it should implement this interface.
/// </summary>
public interface IGenericSerializer : ISerializer
{

    /// <summary>
    /// Try and specialize the serializer for the given type. If the serializer can't specialize for the given type,
    /// it should return false. The returned serializer can be either fixed or variable size, but should not itself be
    /// a generic serializer.
    /// </summary>
    /// <param name="baseType"></param>
    /// <param name="argTypes"></param>
    /// <param name="serializerFinder"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
    public bool TrySpecialize(Type baseType, Type[] argTypes, Func<Type, ISerializer> serializerFinder, [NotNullWhen(true)] out ISerializer? serializer);
}
