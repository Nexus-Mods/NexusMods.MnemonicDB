using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using Reloaded.Memory.Extensions;
using ZLinq;
using ZLinq.Linq;

namespace NexusMods.MnemonicDB.Abstractions;


public class Datoms : List<Datom>
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
    
    public void Add(Datom datom)
    {
        base.Add(datom);
    }

    public void AddTxFn(Action<Datoms, IDb> txFn)
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
        Add(new Datom(prefix, converted));
    }

    /// <summary>
    /// Adds a txFunction to the datoms list, this will be encoded as a special datom whos's value is the txFunction.
    /// </summary>
    public void Add(ITxFunction txFunction)
    {
        Add(Datom.Create(EntityId.MinValueNoPartition, AttributeId.Max, ValueTag.TxFunction, txFunction));
    }

    /// <summary>
    /// Adds all the datoms in the given list to the datoms list.
    /// </summary>
    /// <param name="datoms"></param>
    public void Add(List<Datom> datoms)
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
        Add(Datom.Create(spanDatomLike));
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
    
    /// <summary>
    /// Assumes that the list is sorted by AttributeId, selects the range of datoms that match the attributeId
    /// </summary>
    public ReadOnlySpan<Datom> Range(AttributeId attrId)
    {
        var startIdx = FindRangeStart(attrId);
        if (startIdx == -1)
            return ReadOnlySpan<Datom>.Empty;

        var endIdx = FindRangeEnd(attrId, startIdx);
        return CollectionsMarshal.AsSpan(this).SliceFast(startIdx, endIdx - startIdx);
    }

    public IEnumerable<THighLevel> GetAllResolved<THighLevel, TLowLevel, TSerializer>(
        Attribute<THighLevel, TLowLevel, TSerializer> attr, AttributeResolver resolver) 
        where THighLevel : notnull 
        where TLowLevel : notnull 
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = resolver.AttributeCache.GetAttributeId(attr.Id);
        var range = Range(attrId);
        var result = GC.AllocateUninitializedArray<THighLevel>(range.Length);
        for (var index = 0; index < range.Length; index++)
        {
            var datom = range[index];
            result[index] = attr.FromLowLevel((TLowLevel)datom.Value, resolver);
        }
        return result;
    }

    public THighLevel GetResolved<THighLevel, TLowLevel, TSerializer>(
        Attribute<THighLevel, TLowLevel, TSerializer> attr, AttributeResolver resolver)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = resolver.AttributeCache.GetAttributeId(attr.Id);
        var startIdx = FindRangeStart(attrId);
        if (startIdx == -1)
            throw new KeyNotFoundException($"Attribute not found in datoms: {attr.Id}");
        return attr.FromLowLevel((TLowLevel)this[startIdx].Value, resolver);
    }

    public bool TryGetResolved<THighLevel, TLowLevel, TSerializer>(
        Attribute<THighLevel, TLowLevel, TSerializer> attr, AttributeResolver resolver, out THighLevel value)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = resolver.AttributeCache.GetAttributeId(attr.Id);
        var startIdx = FindRangeStart(attrId);
        if (startIdx == -1)
        {
            value = default!;
            return false;
        }
        value = attr.FromLowLevel((TLowLevel)this[startIdx].Value, resolver);
        return true;
    }
    private int FindRangeStart(AttributeId attrId)
    {
        var left = 0;
        var right = Count - 1;

        while (left <= right)
        {
            var mid = (left + right) / 2;
            var midAttr = this[mid].Prefix.A;

            if (midAttr == attrId && (mid == 0 || this[mid - 1].Prefix.A < attrId))
                return mid;

            if (midAttr < attrId)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return -1;
    }

    private int FindRangeEnd(AttributeId attrId, int startIdx)
    {
        var left = startIdx;
        var right = Count - 1;

        while (left <= right)
        {
            var mid = (left + right) / 2;
            var midAttr = this[mid].Prefix.A;

            if (midAttr == attrId && (mid == Count - 1 || this[mid + 1].Prefix.A > attrId))
                return mid + 1;

            if (midAttr <= attrId)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return startIdx + 1;
    }

    public IEnumerable<ResolvedDatom> Resolved(AttributeResolver attributeResolver)
    {
        foreach (var datom in this)
            yield return new ResolvedDatom(datom, attributeResolver);
    }
    
    public IEnumerable<ResolvedDatom> Resolved(IConnection conn)
    {
        var resolver = conn.AttributeResolver;
        foreach (var datom in this)
            yield return new ResolvedDatom(datom, resolver);
    }
}
