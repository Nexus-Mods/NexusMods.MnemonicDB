using System;

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
}
