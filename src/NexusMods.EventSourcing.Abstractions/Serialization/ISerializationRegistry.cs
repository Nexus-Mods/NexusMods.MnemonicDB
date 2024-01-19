using System;
using System.Buffers;

namespace NexusMods.EventSourcing.Abstractions.Serialization;

/// <summary>
/// A registry of serializers, this class can be queried at runtime to get a serializer for a given type.
/// The registry is populated by the DI container, and the GetSerializer method is backed by a cache, so
/// calling it in inner loops is not a problem.
/// </summary>
public interface ISerializationRegistry
{
    /// <summary>
    /// Gets a serializer that can serialize the given type.
    /// </summary>
    /// <param name="serializedType"></param>
    /// <returns></returns>
    public ISerializer GetSerializer(Type serializedType);

    /// <summary>
    /// Register a serializer for a given type, used to override the default serializers.
    /// </summary>
    /// <param name="serializedType"></param>
    /// <param name="serializer"></param>
    public void RegisterSerializer(Type serializedType, ISerializer serializer);

    /// <summary>
    /// Serializes the given value into the given buffer writer.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <typeparam name="TVal"></typeparam>
    public void Serialize<TVal>(IBufferWriter<byte> writer, TVal value);

    /// <summary>
    /// Deserializes the given bytes into the given type, returning the number of bytes read.
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="value"></param>
    /// <typeparam name="TVal"></typeparam>
    /// <returns></returns>
    public int Deserialize<TVal>(ReadOnlySpan<byte> bytes, out TVal value);
}
