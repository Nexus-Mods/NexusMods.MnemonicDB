using System;
using System.Runtime.InteropServices;
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
    public static ISliceDescriptor Create<THighLevel>(IWritableAttribute<THighLevel> attr, THighLevel value, AttributeCache attributeCache)
    {
        var id = attributeCache.GetAttributeId(attr.Id);
        var tag = attributeCache.GetValueTag(id);
        if (tag != ValueTag.Reference && !attributeCache.IsIndexed(id))
            throw new InvalidOperationException($"Attribute {attr.Id} must be indexed or a reference");

        if (tag == ValueTag.Reference)
            return new BackRefSlice(id, (EntityId)(object)value!);
        
        var index = attr.IsReference ? IndexType.VAETCurrent : IndexType.AVETCurrent;
        return new SliceDescriptor
        {
            From = Datom(EntityId.MinValueNoPartition, attr, value, TxId.MinValue, false, attributeCache, index),
            To = Datom(EntityId.MaxValueNoPartition, attr, value, TxId.MaxValue, false, attributeCache, index),
            IsReverse = false
        };
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
    public static SliceDescriptor Create(IndexType index)
    {
        if (index is IndexType.VAETCurrent or IndexType.VAETHistory)
        {
            // VAET has a special case where we need to include the reference type and an actual reference
            // in the slice
            var from = GC.AllocateUninitializedArray<byte>(KeyPrefix.Size + sizeof(ulong));
            from.AsSpan().Clear();

            var fromPrefix = new KeyPrefix(EntityId.MinValueNoPartition, AttributeId.Min, TxId.MinValue, false, ValueTag.Reference);
            MemoryMarshal.Write(from, fromPrefix);


            var to = GC.AllocateUninitializedArray<byte>(KeyPrefix.Size + sizeof(ulong));
            to.AsSpan().Fill(byte.MaxValue);

            var toPrefix = new KeyPrefix(EntityId.MaxValueNoPartition, AttributeId.Max, TxId.MaxValue, true, ValueTag.Reference);
            MemoryMarshal.Write(to, toPrefix);

            return new SliceDescriptor
            {
                From = new Datom(from, Null.Instance).With(index),
                To = new Datom(to).With(index),
                IsReverse = false
            };
        }
        else
        {
            var from = GC.AllocateUninitializedArray<byte>(KeyPrefix.Size);
            from.AsSpan().Clear();
            var to = GC.AllocateUninitializedArray<byte>(KeyPrefix.Size);
            to.AsSpan().Fill(byte.MaxValue);
            return new SliceDescriptor
            {
                From = new Datom(from).WithIndex(index),
                To = new Datom(to).WithIndex(index),
                IsReverse = false
            };
        }

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
    /// Creates a with a value from the given attribute and value
    /// </summary>
    public static Datom Datom<THighLevel>(EntityId e, IWritableAttribute<THighLevel> a, THighLevel value, TxId tx, bool isRetract, AttributeCache attributeCache, 
        IndexType indexType = IndexType.None)
    {
        // TODO: optimize this
        using var pooled = new PooledMemoryBufferWriter();
        a.Write(e, attributeCache, value, tx, isRetract, pooled);
        return new Datom(pooled.WrittenMemory.ToArray()).WithIndex(indexType);
    }

    /// <summary>
    /// Creates a slice descriptor for the given entity range, for the current EAVT index
    /// </summary>
    public static SliceDescriptor Create(EntityId from, EntityId to)
    {
        return new SliceDescriptor
        {
            From = Datom(from, AttributeId.Min, TxId.MinValue, false, IndexType.EAVTCurrent),
            To = Datom(to, AttributeId.Max, TxId.MaxValue, false, IndexType.EAVTCurrent),
            IsReverse = false
        };
    }
    
    /// <summary>
    /// Creates a new slice descriptor for the given reference attribute and prefix value tag
    /// </summary>
    public static SliceDescriptor Create(AttributeId referenceAttribute, ValueTag prefixValueTag, ReadOnlyMemory<byte> datomValueSpan)
    {
        var fromPrefix = new KeyPrefix(EntityId.MinValueNoPartition, referenceAttribute, TxId.MinValue, false, prefixValueTag, IndexType.AVETCurrent);
        var toPrefix = new KeyPrefix(EntityId.MaxValueNoPartition, referenceAttribute, TxId.MaxValue, false, prefixValueTag, IndexType.AVETCurrent);

        return new SliceDescriptor
        {
            From = new Datom(fromPrefix, datomValueSpan),
            To = new Datom(toPrefix, datomValueSpan),
            IsReverse = false
        };
    }

    public static AllEntitiesInPartition AllEntities(PartitionId partition) 
    {
        return new AllEntitiesInPartition(partition);
    }

    public static AttributeStartingAtId AttributesStartingAt(AttributeId attrId, EntityId id)
    {
        return new AttributeStartingAtId(attrId, id);
    }
}
