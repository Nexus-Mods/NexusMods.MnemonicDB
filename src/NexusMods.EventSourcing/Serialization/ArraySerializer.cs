using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using NexusMods.EventSourcing.Abstractions.Serialization;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// A serializer for arrays of any type supported by the registry.
/// </summary>
public class GenericArraySerializer : IGenericSerializer
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
        if (!baseType.IsArray)
        {
            serializer = null;
            return false;
        }

        var itemType = baseType.GetElementType()!;
        var itemSerializer = serializerFinder(itemType);

        if (itemSerializer.TryGetFixedSize(itemType, out var itemSize))
        {
            var type = typeof(FixedItemSizeArraySerializer<,>).MakeGenericType(itemType, itemSerializer.GetType());
            serializer = (ISerializer) Activator.CreateInstance(type, itemSerializer, itemSize)!;
            return true;
        }
        else
        {
            var type = typeof(VariableItemSizeSerializer<,>).MakeGenericType(itemType, itemSerializer.GetType());
            serializer = (ISerializer) Activator.CreateInstance(type, itemSerializer)!;
            return true;
        }
    }
}


/// <summary>
/// Specialized serializer for arrays of a given type, where the type is of a fixed size.
/// </summary>
/// <param name="itemSerializer"></param>
/// <param name="itemSize"></param>
/// <typeparam name="TItem"></typeparam>
/// <typeparam name="TItemSerializer"></typeparam>
public class FixedItemSizeArraySerializer<TItem, TItemSerializer>(TItemSerializer itemSerializer, int itemSize) : IVariableSizeSerializer<TItem[]>
    where TItemSerializer : IFixedSizeSerializer<TItem>
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType)
    {
        if (!valueType.IsArray)
        {
            return false;
        }

        return itemSerializer.CanSerialize(valueType.GetElementType()!);
    }

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 0;
        return false;
    }

    /// <inheritdoc />
    public void Serialize<TWriter>(TItem[] value, TWriter output) where TWriter : IBufferWriter<byte>
    {
        var totalSize = sizeof(ushort) + (itemSize * value.Length);
        var span = output.GetSpan(totalSize);
        BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)value.Length);

        var offset = sizeof(ushort);
        foreach (var item in value)
        {
            itemSerializer.Serialize(item, span.SliceFast(offset, itemSize));
            offset += itemSize;
        }
        output.Advance(totalSize);
    }

    /// <inheritdoc />
    public int Deserialize(ReadOnlySpan<byte> from, out TItem[] value)
    {
        var size = BinaryPrimitives.ReadUInt16BigEndian(from);
        var array = GC.AllocateUninitializedArray<TItem>(size);

        from = from.SliceFast(sizeof(ushort));
        for (var i = 0; i < size; i++)
        {
            array[i] = itemSerializer.Deserialize(from.SliceFast(i * itemSize, itemSize));
        }

        value = array;
        return sizeof(ushort) + (itemSize * size);
    }
}

/// <summary>
/// Specialized serializer for arrays of a given type, where the type is of a variable size.
/// </summary>
/// <param name="itemSerializer"></param>
/// <typeparam name="TItem"></typeparam>
/// <typeparam name="TItemSerializer"></typeparam>
public class VariableItemSizeSerializer<TItem, TItemSerializer>(TItemSerializer itemSerializer) : IVariableSizeSerializer<TItem[]>
    where TItemSerializer : IVariableSizeSerializer<TItem>
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType)
    {
        if (!valueType.IsArray)
        {
            return false;
        }

        return itemSerializer.CanSerialize(valueType.GetElementType()!);
    }

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 0;
        return false;
    }

    /// <inheritdoc />
    public void Serialize<TWriter>(TItem[] value, TWriter output) where TWriter : IBufferWriter<byte>
    {
        var span = output.GetSpan(sizeof(ushort));
        BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)value.Length);
        output.Advance(sizeof(ushort));

        foreach (var item in value)
        {
            itemSerializer.Serialize(item, output);
        }
    }

    /// <inheritdoc />
    public int Deserialize(ReadOnlySpan<byte> from, out TItem[] value)
    {
        var size = BinaryPrimitives.ReadUInt16BigEndian(from);
        var array = GC.AllocateUninitializedArray<TItem>(size);

        var offset = sizeof(ushort);
        for (var i = 0; i < size; i++)
        {
            offset += itemSerializer.Deserialize(from.SliceFast(offset), out var item);
            array[i] = item;
        }

        value = array;
        return offset;
    }
}
