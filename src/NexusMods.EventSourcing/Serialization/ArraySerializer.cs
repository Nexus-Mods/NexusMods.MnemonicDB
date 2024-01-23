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
public class FixedItemSizeArraySerializer<TItem, TItemSerializer>(TItemSerializer itemSerializer, int itemSize) : AVariableSizeSerializer<TItem[]>
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
    public override void Serialize<TWriter>(TItem[] value, TWriter output)
    {
        WriteLength(output, value.Length);
        var totalSize = itemSize * value.Length;
        var span = output.GetSpan(totalSize);

        var offset = 0;
        foreach (var item in value)
        {
            itemSerializer.Serialize(item, span.SliceFast(offset, itemSize));
            offset += itemSize;
        }
        output.Advance(totalSize);
    }

    /// <inheritdoc />
    public override int Deserialize(ReadOnlySpan<byte> from, out TItem[] value)
    {
        var lengthSize = ReadLength(from, out var length);
        var array = GC.AllocateUninitializedArray<TItem>(length);

        from = from.SliceFast(lengthSize);
        for (var i = 0; i < length; i++)
        {
            array[i] = itemSerializer.Deserialize(from.SliceFast(0, itemSize));
            from = from.SliceFast(itemSize);
        }

        value = array;
        return lengthSize + (itemSize * length);
    }
}

/// <summary>
/// Specialized serializer for arrays of a given type, where the type is of a variable size.
/// </summary>
/// <param name="itemSerializer"></param>
/// <typeparam name="TItem"></typeparam>
/// <typeparam name="TItemSerializer"></typeparam>
public class VariableItemSizeSerializer<TItem, TItemSerializer>(TItemSerializer itemSerializer) : AVariableSizeSerializer<TItem[]>
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
    public override void Serialize<TWriter>(TItem[] value, TWriter output)
    {
        WriteLength(output, value.Length);

        foreach (var item in value)
        {
            itemSerializer.Serialize(item, output);
        }
    }

    /// <inheritdoc />
    public override int Deserialize(ReadOnlySpan<byte> from, out TItem[] value)
    {
        var lengthSize = ReadLength(from, out var size);
        var array = GC.AllocateUninitializedArray<TItem>(size);

        var offset = lengthSize;
        for (var i = 0; i < size; i++)
        {
            offset += itemSerializer.Deserialize(from.SliceFast(offset), out var item);
            array[i] = item;
        }

        value = array;
        return offset;
    }
}
