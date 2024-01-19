using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for EntityIds.
/// </summary>
public class EntityIdSerializer : IFixedSizeSerializer<EntityId>
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType)
    {
        return valueType == typeof(EntityId);
    }

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 16;
        return true;
    }

    /// <inheritdoc />
    public void Serialize(EntityId value, Span<byte> output)
    {
        value.TryWriteBytes(output);
    }

    /// <inheritdoc />
    public EntityId Deserialize(ReadOnlySpan<byte> from)
    {
        return EntityId.From(from);
    }
}

/// <summary>
/// Serializer for typed EntityIds.
/// </summary>
public class GenericEntityIdSerializer : IGenericSerializer
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType)
    {
        return false;
    }

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 0;
        return false;
    }

    /// <inheritdoc />
    public bool TrySpecialize(Type baseType, Type[] argTypes, Func<Type, ISerializer> serializerFinder, [NotNullWhen(true)] out ISerializer? serializer)
    {
        if (baseType != typeof(EntityId<>) || argTypes.Length != 1)
        {
            serializer = null;
            return false;
        }

        var type = typeof(EntityIdSerializer<>).MakeGenericType(argTypes[0]);
        serializer = (ISerializer) Activator.CreateInstance(type)!;
        return true;
    }
}

internal class EntityIdSerializer<T> : IFixedSizeSerializer<EntityId<T>> where T : IEntity
{
    public bool CanSerialize(Type valueType)
    {
        return valueType == typeof(EntityId<T>);
    }

    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 16;
        return true;
    }

    public void Serialize(EntityId<T> value, Span<byte> output)
    {
        value.Value.TryWriteBytes(output);
    }

    public EntityId<T> Deserialize(ReadOnlySpan<byte> from)
    {
        return EntityId<T>.From(from);
    }
}
