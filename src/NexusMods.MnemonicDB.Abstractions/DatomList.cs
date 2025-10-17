using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.MnemonicDB.Abstractions;

public class DatomList : List<ValueDatom>, IDatomsListLike
{
    public DatomList(AttributeCache attributeCache) : base()
    {
        AttributeCache = attributeCache;
    }

    public static DatomList Create<TEnumerator, TSlice>(TEnumerator enumerator, TSlice slice, AttributeCache attributeCache)
        where TEnumerator : IRefDatomEnumerator, allows ref struct
        where TSlice : ISliceDescriptor, allows ref struct
    {
        var datoms = new DatomList(attributeCache)
        {
            { enumerator, slice }
        };
        return datoms;
    }
    
    public static DatomList Create<TFactory, TEnum, TSlice>(in TFactory factory, TSlice slice, AttributeCache attributeCache, bool totalOrdered = false)
        where TFactory : IRefDatomEnumeratorFactory<TEnum>, allows ref struct
        where TSlice : ISliceDescriptor, allows ref struct
        where TEnum : IRefDatomEnumerator
    {
        using var enumerator = factory.GetRefDatomEnumerator(totalOrdered);
        return Create(enumerator, slice, attributeCache);
    }

    public DatomList(IDb db)
    {
        AttributeCache = db.AttributeCache;
    }

    public DatomList(IDatomStore store)
    {
        AttributeCache = store.AttributeCache;
    }

    public List<ValueDatom> Datoms => this;

    [field: AllowNull, MaybeNull] public AttributeCache AttributeCache { get; }
}

public interface IDatomsListLike : IEnumerable<ValueDatom>
{
    public List<ValueDatom> Datoms { get; }

    public AttributeCache AttributeCache { get; }
}

public static class DatomListLikeExtensions
{
    public static int Count(this IDatomsListLike lst) => lst.Datoms.Count;

    public static void Add(this IDatomsListLike lst, IDatomLikeRO datomLike)
    {
        lst.Add(datomLike);
    }

    public static void Add<THighLevel, TLowLevel, TSerializer>(this IDatomsListLike lst, EntityId e,
        Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        lst.Add(e, attr, value, false, TxId.Tmp);
    }

    public static void Retract<THighLevel, TLowLevel, TSerializer>(this IDatomsListLike lst, EntityId e,
        Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        lst.Add(e, attr, value, true, TxId.Tmp);
    }

    public static void Add<THighLevel, TLowLevel, TSerializer>(this IDatomsListLike lst, EntityId e,
        Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, bool isRetract)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        lst.Add(e, attr, value, isRetract, TxId.Tmp);
    }

    public static void Add<THighLevel, TLowLevel, TSerializer>(this IDatomsListLike lst, EntityId e,
        Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, bool isRetract, TxId txId)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = lst.AttributeCache!.GetAttributeId(attr.Id);
        var prefix = new KeyPrefix(e, attrId, txId, isRetract, attr.LowLevelType);
        var converted = attr.ToLowLevel(value);
        lst.Datoms.Add(new ValueDatom(prefix, converted));
    }

    /// <summary>
    /// Adds a txFunction to the datoms list, this will be encoded as a special datom whos's value is the txFunction.
    /// </summary>
    public static void Add(this IDatomsListLike lst, ITxFunction txFunction)
    {
        lst.Datoms.Add(
            ValueDatom.Create(EntityId.MinValueNoPartition, AttributeId.Max, ValueTag.TxFunction, txFunction));
    }

    /// <summary>
    /// Adds all the datoms in the given list to the datoms list.
    /// </summary>
    /// <param name="datoms"></param>
    public static void Add(this IDatomsListLike lst, List<ValueDatom> datoms)
    {
        lst.Datoms.AddRange(datoms);
    }

    public static void Add<TEnumerator, TSlice>(this IDatomsListLike lst, TEnumerator datoms, TSlice slice)
        where TEnumerator : IRefDatomEnumerator, allows ref struct
        where TSlice : ISliceDescriptor, allows ref struct
    {
        while (datoms.MoveNext(slice))
            lst.Add(datoms);

    }

    public static void Add<TEnum>(this IDatomsListLike lst, in TEnum spanDatomLike)
        where TEnum : ISpanDatomLikeRO, allows ref struct
    {
        lst.Add(ValueDatom.Create(spanDatomLike));
    }

    public static bool TryGetOne(this IDatomsListLike lst, IAttribute attr, out object value)
    {
        var id = lst.AttributeCache.GetAttributeId(attr.Id);
        foreach (var t in lst.Datoms)
        {
            if (t.Prefix.A != id) continue;
            value = t.Value;
            return true;
        }
        value = default!;
        return false;
    }

    public static bool Contains(this IDatomsListLike lst, IAttribute attr)
    {
        var attrId = lst.AttributeCache.GetAttributeId(attr.Id);
        foreach (var t in lst.Datoms)
            if (t.Prefix.A == attrId)
                return true;
        return false;
    }
}
