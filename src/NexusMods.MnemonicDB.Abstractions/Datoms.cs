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

public class Datoms : List<ValueDatom>
{
    public Datoms(AttributeCache attributeCache) : base()
    {
        AttributeCache = attributeCache;
    }

    public static Datoms Create<TEnumerator, TSlice>(TEnumerator enumerator, TSlice slice, AttributeCache attributeCache)
        where TEnumerator : IRefDatomEnumerator, allows ref struct
        where TSlice : ISliceDescriptor, allows ref struct
    {
        var datoms = new Datoms(attributeCache)
        {
            { enumerator, slice }
        };
        return datoms;
    }
    
    public static Datoms Create<TFactory, TEnum, TSlice>(in TFactory factory, TSlice slice, AttributeCache attributeCache, bool totalOrdered = false)
        where TFactory : IRefDatomEnumeratorFactory<TEnum>, allows ref struct
        where TSlice : ISliceDescriptor, allows ref struct
        where TEnum : IRefDatomEnumerator
    {
        using var enumerator = factory.GetRefDatomEnumerator(totalOrdered);
        return Create(enumerator, slice, attributeCache);
    }

    public Datoms(IDb db)
    {
        AttributeCache = db.AttributeCache;
    }

    public Datoms(IDatomStore store)
    {
        AttributeCache = store.AttributeCache;
    }

    [field: AllowNull, MaybeNull] public AttributeCache AttributeCache { get; }
    
    public void Add(ValueDatom datomLike)
    {
        Add(datomLike);
    }

    public void TxFn(Action<Datoms, IDb> txFn)
    {
        throw new NotImplementedException();
    }

    public void Add<THighLevel, TLowLevel, TSerializer>(EntityId e,
        Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        Add(e, attr, value, false, TxId.Tmp);
    }

    public void Retract<THighLevel, TLowLevel, TSerializer>(EntityId e,
        Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        Add(e, attr, value, true, TxId.Tmp);
    }

    public void Add<THighLevel, TLowLevel, TSerializer>(EntityId e,
        Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, bool isRetract)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        Add(e, attr, value, isRetract, TxId.Tmp);
    }

    public void Add<THighLevel, TLowLevel, TSerializer>(EntityId e,
        Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, bool isRetract, TxId txId)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = AttributeCache!.GetAttributeId(attr.Id);
        var prefix = new KeyPrefix(e, attrId, txId, isRetract, attr.LowLevelType);
        var converted = attr.ToLowLevel(value);
        Add(new ValueDatom(prefix, converted));
    }

    /// <summary>
    /// Adds a txFunction to the datoms list, this will be encoded as a special datom whos's value is the txFunction.
    /// </summary>
    public void Add(ITxFunction txFunction)
    {
        Add(ValueDatom.Create(EntityId.MinValueNoPartition, AttributeId.Max, ValueTag.TxFunction, txFunction));
    }

    /// <summary>
    /// Adds all the datoms in the given list to the datoms list.
    /// </summary>
    /// <param name="datoms"></param>
    public void Add(List<ValueDatom> datoms)
    {
        AddRange(datoms);
    }

    public void Add<TEnumerator, TSlice>(TEnumerator datoms, TSlice slice)
        where TEnumerator : IRefDatomEnumerator, allows ref struct
        where TSlice : ISliceDescriptor, allows ref struct
    {
        while (datoms.MoveNext(slice))
            Add(datoms);
    }

    public void Add<TEnum>(in TEnum spanDatomLike)
        where TEnum : ISpanDatomLikeRO, allows ref struct
    {
        Add(ValueDatom.Create(spanDatomLike));
    }

    public bool TryGetOne(IAttribute attr, out object value)
    {
        var id = AttributeCache.GetAttributeId(attr.Id);
        for (var index = 0; index < Count; index++)
        {
            var t = this[index];
            if (t.Prefix.A != id) continue;
            value = t.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public bool Contains(IAttribute attr)
    {
        var attrId = AttributeCache.GetAttributeId(attr.Id);
        for (var index = 0; index < Count; index++)
        {
            var t = this[index];
            if (t.Prefix.A == attrId)
                return true;
        }

        return false;
    }
}
