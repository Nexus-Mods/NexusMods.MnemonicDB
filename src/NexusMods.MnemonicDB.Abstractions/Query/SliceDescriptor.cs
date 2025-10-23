using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Query;

/// <summary>
/// A slice descriptor for querying datoms, it doesn't contain any data, but can be combined
/// with other objects like databases or indexes to query for datoms.
/// </summary>
public static class SliceDescriptor
{
    /// <summary>
    /// Creates a slice descriptor for the given entity in the current EAVT index
    /// </summary>
    public static EntityIdSlice Create(EntityId e)
    {
        return new EntityIdSlice(e);
    }

    /// <summary>
    /// Creates a slice descriptor for the given transaction in the TxLog index
    /// </summary>
    public static TxIdSlice Create(TxId tx)
    {
        return new TxIdSlice(tx);
    }

    /// <summary>
    /// Creates a slice descriptor for the given attribute in the current AVET index
    /// </summary>
    [MustDisposeResource]
    public static IndexedValueSlice Create<THighLevel, TLowLevel, TSerializer>(Attribute<THighLevel, TLowLevel, TSerializer> attr, THighLevel value, AttributeCache attributeCache) 
        where THighLevel : notnull 
        where TLowLevel : notnull 
        where TSerializer : IValueSerializer<TLowLevel>
    {
        var converted = attr.ToLowLevel(value);
        var attrId = attributeCache.GetAttributeId(attr.Id);
        return IndexedValueSlice.Create(attrId, converted, attributeCache);
    }
    
    /// <summary>
    /// Creates a slice descriptor for the given reference attribute and entity that is being pointed to.
    /// </summary>
    public static BackRefSlice Create(ReferenceAttribute attr, EntityId value, AttributeCache attributeCache)
    {
        var id = attributeCache.GetAttributeId(attr.Id);
        return new BackRefSlice(id, value);
    }

    /// <summary>
    /// Creates a slice descriptor for the given reference attribute and entity that is being pointed to, this is a
    /// reverse lookup.
    /// </summary>
    public static BackRefSlice Create(AttributeId referenceAttribute, EntityId pointingTo)
    {
        return new BackRefSlice(referenceAttribute, pointingTo);
    }
    
    /// <summary>
    /// Creates a slice descriptor for the given attribute from the current AEVT index
    /// reverse lookup.
    /// </summary>
    public static AttributeSlice Create(AttributeId referenceAttribute)
    {
        return new AttributeSlice(referenceAttribute);
    }


    /// <summary>
    /// Creates a slice descriptor for the given attribute and entity from the EAVT index
    /// </summary>
    public static EASlice Create(EntityId e, AttributeId a)
    {
        return new EASlice(e, a);
    }
    
    /// <summary>
    /// Creates a slice descriptor for the given attribute in the current AEVT index
    /// </summary>
    public static AttributeSlice Create(IAttribute attr, AttributeCache attributeCache)
    {
        var attrId = attributeCache.GetAttributeId(attr.Id);
        return new AttributeSlice(attrId);
    }


    /// <summary>
    /// Creates a slice descriptor for datoms that reference the given entity via the VAET index
    /// </summary>
    public static ReferencesSlice CreateReferenceTo(EntityId pointingTo) 
        => new(pointingTo);


    /// <summary>
    /// Creates a slice descriptor for the entire index
    /// </summary>
    public static IndexSlice Create(IndexType index)
    {
        return new IndexSlice(index);
    }

    /// <summary>
    /// Creates a datom with no value from the given parts
    /// </summary>
    public static Datom Datom(EntityId e, AttributeId a, TxId id, bool isRetract, IndexType indexType = IndexType.None)
    {
        KeyPrefix prefix = new(e, a, id, isRetract, ValueTag.Null, indexType);
        return new Datom(prefix, ReadOnlyMemory<byte>.Empty);
    }
    
    /// <summary>
    /// Creates a slice descriptor for the given entity range, for the current EAVT index
    /// </summary>
    public static EntityRangeSlice Create(EntityId from, EntityId to)
    {
        return new EntityRangeSlice(from, to);
    }
    
    /// <summary>
    /// Creates a new slice descriptor for the given reference attribute and prefix value tag
    /// </summary>
    [MustDisposeResource]
    public static IndexedValueSlice Create(AttributeId attrId, ValueTag tag, object value)
    {
        return new IndexedValueSlice(attrId, tag, value);
    }

    public static AllEntitiesInPartition AllEntities(PartitionId partition) 
    {
        return new AllEntitiesInPartition(partition);
    }

    public static AttributeStartingAtId AttributesStartingAt(AttributeId attrId, EntityId id)
    {
        return new AttributeStartingAtId(attrId, id);
    }

    /// <summary>
    /// Wraps the slice in a slice that forces it to query the history variant of the same index
    /// </summary>
    public static HistorySlice<TParent> History<TParent>(this TParent parent) where TParent : ISliceDescriptor
    {
        return new HistorySlice<TParent>(parent);
    }
    
    public static readonly AllSlice All = new();
}
