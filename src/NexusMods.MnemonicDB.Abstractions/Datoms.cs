using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Traits;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.MnemonicDB.Abstractions;


public class Datoms : List<Datom>
{
    private readonly AttributeResolver _resolver;
    
    public Datoms(AttributeResolver resolver) : base()
    {
        _resolver = resolver;
    }

    public static Datoms Create<TEnumerator, TSlice>(TEnumerator enumerator, TSlice slice, AttributeResolver attributeCache)
        where TEnumerator : IRefDatomEnumerator, allows ref struct
        where TSlice : ISliceDescriptor, allows ref struct
    {
        var datoms = new Datoms(attributeCache)
        {
            { enumerator, slice }
        };
        return datoms;
    }
    
    public static Datoms Create<TFactory, TEnum, TSlice>(in TFactory factory, TSlice slice, AttributeResolver attributeCache)
        where TFactory : IRefDatomEnumeratorFactory<TEnum>, allows ref struct
        where TSlice : ISliceDescriptor, allows ref struct
        where TEnum : IRefDatomEnumerator
    {
        using var enumerator = factory.GetRefDatomEnumerator();
        return Create(enumerator, slice, attributeCache);
    }

    public Datoms(IDb db)
    {
        _resolver = db.AttributeResolver;
    }
    
    public AttributeCache AttributeCache => _resolver.AttributeCache;
    
    public void Add(Datom datom)
    {
        base.Add(datom);
    }

    public void Add(ITxFunction txFn)
    {
        base.Add(Datom.Create(EntityId.MaxValueNoPartition, AttributeId.Max, ValueTag.TxFunction, txFn));
    }

    public void Add<THighLevel, TLowLevel, TSerializer>(EntityId e,
        Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        Add(e, attr, value, false, TxId.Tmp);
    }
    
    public void Add<THighLevel, TLowLevel, TSerializer>(EntityId e,
        CollectionAttribute<THighLevel, TLowLevel, TSerializer> attr, IEnumerable<THighLevel> values)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        foreach (var value in values)
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
    
    public void Add<TValue>(EntityId e, AttributeId attrId, TValue value, bool isRetract)
    where TValue : notnull
    {
        var tag = _resolver.AttributeCache.GetValueTag(attrId);
        Add(Datom.Create(e, attrId, new TaggedValue(tag, value), TxId.Tmp, isRetract));
    }

    public void Add<THighLevel, TLowLevel, TSerializer>(EntityId e,
        Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, bool isRetract, TxId txId)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = AttributeCache.GetAttributeId(attr.Id);
        var prefix = new KeyPrefix(e, attrId, txId, isRetract, attr.LowLevelType);
        var converted = attr.ToLowLevel(value);
        Add(new Datom(prefix, converted));
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
            value = attr.FromLowLevelObject(t.V, _resolver);
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
    
    public IEnumerable<object> GetAllResolved(IAttribute attr)
    {
        var attrId = _resolver.AttributeCache.GetAttributeId(attr.Id);
        var startIdx = FindRangeStart(attrId);
        if (startIdx == -1)
            yield break;
        
        var stopIdx = FindRangeEnd(attrId, startIdx);
        if (stopIdx == -1)
            stopIdx = Count - 1;
        
        for (var idx = 0; idx <= stopIdx; idx++)
            yield return attr.FromLowLevelObject(this[idx].V, _resolver);
    }

    public IEnumerable<THighLevel> GetAllResolved<THighLevel, TLowLevel, TSerializer>(
        Attribute<THighLevel, TLowLevel, TSerializer> attr) 
        where THighLevel : notnull 
        where TLowLevel : notnull 
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = _resolver.AttributeCache.GetAttributeId(attr.Id);
        var startIdx = FindRangeStart(attrId);
        if (startIdx == -1)
            yield break;
        
        var stopIdx = FindRangeEnd(attrId, startIdx);
        if (stopIdx == -1)
            stopIdx = Count;
        
        for (var idx = startIdx; idx < stopIdx; idx++)
            yield return attr.FromLowLevel((TLowLevel)this[idx].V, _resolver);
    }

    public THighLevel GetResolved<THighLevel, TLowLevel, TSerializer>(
        Attribute<THighLevel, TLowLevel, TSerializer> attr)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = _resolver.AttributeCache.GetAttributeId(attr.Id);
        var startIdx = FindRangeStart(attrId);
        if (startIdx == -1)
            throw new KeyNotFoundException($"Attribute not found in datoms: {attr.Id}");
        return attr.FromLowLevel((TLowLevel)this[startIdx].V, _resolver);
    }
    
    public bool TryGetResolved<THighLevel, TLowLevel, TSerializer>(
        Attribute<THighLevel, TLowLevel, TSerializer> attr, out THighLevel value)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = _resolver.AttributeCache.GetAttributeId(attr.Id);
        var startIdx = FindRangeStart(attrId);
        if (startIdx == -1)
        {
            value = default!;
            return false;
        }
        value = attr.FromLowLevel((TLowLevel)this[startIdx].V, _resolver);
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

    public IEnumerable<ResolvedDatom> Resolved()
    {
        foreach (var datom in this)
            yield return new ResolvedDatom(datom, _resolver);
    }
    
    public IEnumerable<ResolvedDatom> Resolved(IConnection conn)
    {
        var resolver = conn.AttributeResolver;
        foreach (var datom in this)
            yield return new ResolvedDatom(datom, resolver);
    }

    public IEnumerable<TModel> AsModels<TModel>(IDb db) 
        where TModel : IReadOnlyModel<TModel>
    {
        foreach (var datom in this)
            yield return TModel.Create(db, datom.Prefix.E);
    }
}
