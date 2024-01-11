using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NexusMods.EventSourcing.Abstractions.Serialization;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Manages ISerializer instances, specializes them and manages a cache for them for serialized types.
/// </summary>
public class SerializationRegistry : ISerializationRegistry
{
    private ConcurrentDictionary<Type, ISerializer> _cachedSerializers = new();
    private readonly ISerializer[] _diInjectedSerializers;
    private readonly IGenericSerializer[] _genericSerializers;
    private readonly GenericArraySerializer _arraySerializer;

    /// <summary>
    /// DI constructor.
    /// </summary>
    /// <param name="diInjectedSerializers"></param>
    public SerializationRegistry(IEnumerable<ISerializer> diInjectedSerializers)
    {
        _diInjectedSerializers = diInjectedSerializers.ToArray();
        _genericSerializers = _diInjectedSerializers.OfType<IGenericSerializer>().ToArray();
        _arraySerializer = _diInjectedSerializers.OfType<GenericArraySerializer>().First();
    }

    /// <summary>
    /// Gets a serializer that can serialize the given type.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="recursiveGetSerializer">Called when the cache needs to recursively create another type serializer</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public ISerializer GetSerializer(Type type)
    {
        TOP:
        if (_cachedSerializers.TryGetValue(type, out var found))
            return found;

        var result = _diInjectedSerializers.FirstOrDefault(s => s.CanSerialize(type));
        if (result != null)
        {
            return result;
        }

        if (type.IsConstructedGenericType)
        {
            foreach (var maker in _genericSerializers)
            {
                if (maker.TrySpecialize(type.GetGenericTypeDefinition(),
                        type.GetGenericArguments(), GetSerializer, out var serializer))
                {
                    return serializer;
                }
            }
        }

        if (type.IsArray)
        {
            _arraySerializer.TrySpecialize(type, [type.GetElementType()!], GetSerializer, out var serializer);

            if (!_cachedSerializers.TryAdd(type, serializer!))
                goto TOP;
            return serializer!;
        }

        throw new Exception($"No serializer found for {type}");
    }

    /// <summary>
    /// Adds a serializer to the registry.
    /// </summary>
    /// <param name="serializedType"></param>
    /// <param name="serializer"></param>
    public void RegisterSerializer(Type serializedType, ISerializer serializer)
    {
        _cachedSerializers.TryAdd(serializedType, serializer);
    }


    /// <inheritdoc />
    public void Serialize<TVal>(IBufferWriter<byte> writer, TVal value)
    {
        var serializer = GetSerializer(typeof(TVal));
        if (serializer.TryGetFixedSize(typeof(TVal), out var size) && serializer is IFixedSizeSerializer<TVal> fixedSizeSerializer)
        {
            var span = writer.GetSpan(size);
            fixedSizeSerializer.Serialize(value, span);
            writer.Advance(size);
        }
        else if (serializer is IVariableSizeSerializer<TVal> variableSizeSerializer)
        {
            variableSizeSerializer.Serialize(value, writer);
        }
        else
        {
            throw new Exception($"Unknown serializer type {serializer.GetType().Name}");
        }

    }

    /// <inheritdoc />
    public int Deserialize<TVal>(ReadOnlySpan<byte> bytes, out TVal value)
    {
        var serializer = GetSerializer(typeof(TVal));
        if (serializer.TryGetFixedSize(typeof(TVal), out var size) && serializer is IFixedSizeSerializer<TVal> fixedSizeSerializer)
        {
            value = fixedSizeSerializer.Deserialize(bytes.SliceFast(0, size));
            return size;
        }
        else if (serializer is IVariableSizeSerializer<TVal> variableSizeSerializer)
        {
            return variableSizeSerializer.Deserialize(bytes, out value);
        }
        else
        {
            throw new Exception($"Unknown serializer type {serializer.GetType().Name}");
        }
    }
}
