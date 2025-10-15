using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Traits;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.MnemonicDB.Abstractions;

public class DatomList : List<IDatomLikeRO>, IDatomsListLike
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

    public List<IDatomLikeRO> Datoms => this;

    [field: AllowNull, MaybeNull] public AttributeCache AttributeCache { get; }
}

public interface IDatomsListLike
{
    public List<IDatomLikeRO> Datoms { get; }
    
    public AttributeCache AttributeCache { get; }
    
    public void Add<THighLevel, TLowLevel, TSerializer>(EntityId e, Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, bool isRetract = false, TxId? txId = null) 
        where THighLevel : notnull 
        where TLowLevel : notnull
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var attrId = AttributeCache!.GetAttributeId(attr.Id);
        var prefix = new KeyPrefix(e, attrId, txId ?? TxId.Tmp, isRetract, attr.LowLevelType);
        var converted = attr.ToLowLevel(value);
        Datoms.Add(new ValueDatom<TLowLevel>(prefix, converted));
    }

    /// <summary>
    /// Adds a txFunction to the datoms list, this will be encoded as a special datom whos's value is the txFunction.
    /// </summary>
    /// <param name="txFunction"></param>
    public void Add(ITxFunction txFunction)
    {
        Datoms.Add(ValueDatom.Create(EntityId.MinValueNoPartition, AttributeId.Max, ValueTag.TxFunction, txFunction));
    }

    /// <summary>
    /// Adds all the datoms in the given list to the datoms list.
    /// </summary>
    /// <param name="datoms"></param>
    public void Add(List<IDatomLikeRO> datoms)
    {
        Datoms.AddRange(datoms);
    }
}
