using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    
    public static void Add<THighLevel, TLowLevel, TSerializer>(this IDatomsListLike lst, EntityId e, Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        lst.Add(e, attr, value, false, TxId.Tmp);
    }
    
    public static void Retract<THighLevel, TLowLevel, TSerializer>(this IDatomsListLike lst, EntityId e, Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        lst.Add(e, attr, value, true, TxId.Tmp);
    }
    
    public static void Add<THighLevel, TLowLevel, TSerializer>(this IDatomsListLike lst, EntityId e, Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, bool isRetract)
        where THighLevel : notnull
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        lst.Add(e, attr, value, isRetract, TxId.Tmp);
    }
    
    public static void Add<THighLevel, TLowLevel, TSerializer>(this IDatomsListLike lst, EntityId e, Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, bool isRetract, TxId txId) 
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
        lst.Datoms.Add(ValueDatom.Create(EntityId.MinValueNoPartition, AttributeId.Max, ValueTag.TxFunction, txFunction));
    }

    /// <summary>
    /// Adds all the datoms in the given list to the datoms list.
    /// </summary>
    /// <param name="datoms"></param>
    public static void Add(this IDatomsListLike lst, List<ValueDatom> datoms)
    {
        lst.Datoms.AddRange(datoms);
    }
}
