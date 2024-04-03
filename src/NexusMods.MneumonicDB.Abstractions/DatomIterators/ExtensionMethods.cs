using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB.Abstractions.DatomIterators;

/// <summary>
///     Extension methods for the IIterator interface
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    ///     Seeks to the given entity id in the iterator, assumes other values are 0
    /// </summary>
    public static IIterator SeekTo<TParent>(this TParent parent, EntityId eid)
        where TParent : ISeekableIterator
    {
        var key = new KeyPrefix();
        key.Set(eid, AttributeId.Min, TxId.MinValue, false);
        return parent.SeekTo(ref key);
    }

    /// <summary>
    ///     Seeks to the given tx id in the iterator, assumes other values are 0
    /// </summary>
    public static IIterator SeekTo<TParent>(this TParent parent, TxId txId)
        where TParent : ISeekableIterator
    {
        var key = new KeyPrefix();
        key.Set(EntityId.MinValueNoPartition, AttributeId.Min, txId, false);
        return parent.SeekTo(ref key);
    }

    /// <summary>
    ///     Seeks to the given entity id in the iterator, assumes other values are 0
    /// </summary>
    public static IIterator SeekTo<TParent>(this TParent parent, AttributeId aid)
        where TParent : ISeekableIterator
    {
        var key = new KeyPrefix();
        key.Set(EntityId.MinValueNoPartition, aid, TxId.MinValue, false);
        return parent.SeekTo(ref key);
    }


    /// <summary>
    ///     Seeks to the given attribute and value in the iterator, assumes that the other values are 0
    ///     and that the value is unmanaged
    /// </summary>
    public static IIterator SeekTo<TParent, TVal>(this TParent parent, AttributeId aid, TVal val)
        where TParent : ISeekableIterator
        where TVal : unmanaged
    {
        unsafe
        {
            Span<byte> span = stackalloc byte[sizeof(TVal) + sizeof(KeyPrefix)];
            var key = MemoryMarshal.Cast<byte, KeyPrefix>(span);
            key[0].Set(EntityId.MinValue, aid, TxId.MinValue, false);
            MemoryMarshal.Write(span.SliceFast(sizeof(KeyPrefix)), val);
            return parent.Seek(span);
        }
    }

    /// <summary>
    ///     Seeks to the given attribute and value in the iterator, assumes that the other values are 0
    ///     and that the value is unmanaged
    /// </summary>
    public static IIterator SeekTo<TParent>(this TParent parent, EntityId eid, AttributeId aid)
        where TParent : ISeekableIterator
    {
        var key = new KeyPrefix();
        key.Set(eid, aid, TxId.MinValue, false);
        return parent.SeekTo(ref key);
    }

    /// <summary>
    ///     Seeks to the given attribute and value in the iterator, assumes that the other values are 0
    ///     and that the value is unmanaged
    /// </summary>
    public static IIterator SeekTo<TParent, TAttribute, TVal>(this TParent parent, TVal val)
        where TParent : ISeekableIterator
        where TAttribute : IAttribute<TVal>
        where TVal : unmanaged
    {
        unsafe
        {
            var aid = parent.Registry.GetAttributeId(typeof(TAttribute));
            Span<byte> span = stackalloc byte[sizeof(TVal) + sizeof(KeyPrefix)];
            var key = MemoryMarshal.Cast<byte, KeyPrefix>(span);
            key[0].Set(EntityId.MinValue, aid, TxId.MinValue, false);
            MemoryMarshal.Write(span.SliceFast(sizeof(KeyPrefix)), val);
            return parent.Seek(span);
        }
    }

    /// <summary>
    ///     Seeks to the given key prefix in the iterator, the value is null;
    /// </summary>
    public static IIterator SeekTo<TParent>(this TParent parent, ref KeyPrefix prefix)
        where TParent : ISeekableIterator
    {
        return parent.Seek(MemoryMarshal.CreateSpan(ref prefix, 1).CastFast<KeyPrefix, byte>());
    }

    /// <summary>
    ///     Reverses the order of the iterator so that a .Next will move backwards
    /// </summary>
    public static ReverseIterator<TParent> Reverse<TParent>(this TParent parent)
        where TParent : IIterator
    {
        return new ReverseIterator<TParent>(parent);
    }

    /// <summary>
    ///     Converts the iterator to an IEnumerable of IReadDatom
    /// </summary>
    public static IEnumerable<IReadDatom> Resolve<TParent>(this TParent parent)
        where TParent : IIterator
    {
        var registry = parent.Registry;
        while (parent.Valid)
        {
            yield return registry.Resolve(parent.Current);
            parent.Next();
        }
    }

    /// <summary>
    ///     Gets the current key prefix of the iterator
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeyPrefix CurrentKeyPrefix(this IIterator iterator)
    {
        return KeyPrefix.Read(iterator.Current);
    }

    /// <summary>
    ///     Gets the current key prefix of the iterator as a value, assuming the value is unmanaged
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue CurrentValue<TIterator, TValue>(this TIterator iterator)
        where TIterator : IIterator
        where TValue : unmanaged
    {
        unsafe
        {
            return MemoryMarshal.Read<TValue>(iterator.Current.SliceFast(sizeof(KeyPrefix)));
        }
    }

    /// <summary>
    ///     Keeps the iterator valid while the attribute is equal to the given attribute
    ///     type
    /// </summary>
    public static WhileA<TParent> While<TParent>(this TParent parent, Type a)
        where TParent : IIterator
    {
        var attrId = parent.Registry.GetAttributeId(a);
        return new WhileA<TParent>(attrId, parent);
    }

    /// <summary>
    ///     Keeps the iterator valid while the attribute is equal to the given attribute
    ///     type
    /// </summary>
    public static WhileUnmanagedV<TParent, TVal> WhileUnmanagedV<TParent, TVal>(this TParent parent, TVal tval)
        where TParent : IIterator
        where TVal : unmanaged, IEquatable<TVal>
    {
        return new WhileUnmanagedV<TParent, TVal>(tval, parent);
    }

    /// <summary>
    ///     Keeps the iterator valid while the attribute is equal to the given attribute
    /// </summary>
    public static WhileA<TParent> While<TParent>(this TParent parent, AttributeId a)
        where TParent : IIterator
    {
        return new WhileA<TParent>(a, parent);
    }

    /// <summary>
    ///     Keeps the iterator valid while the attribute is equal to the given attribute
    /// </summary>
    public static WhileTx<TParent> While<TParent>(this TParent parent, TxId txId)
        where TParent : IIterator
    {
        return new WhileTx<TParent>(txId, parent);
    }

    /// <summary>
    ///     Keeps the iterator valid while the attribute is equal to the given attribute
    /// </summary>
    public static IEnumerable<TOut> Select<TParent, TOut>(this TParent parent, Func<TParent, TOut> f)
        where TParent : IIterator
    {
        while (parent.Valid)
        {
            yield return f(parent);
            parent.Next();
        }
    }

    /// <summary>
    ///     Keeps the iterator valid while the attribute is equal to the given attribute
    /// </summary>
    public static WhileE<TParent> While<TParent>(this TParent parent, EntityId e)
        where TParent : IIterator
    {
        return new WhileE<TParent>(e, parent);
    }

}
