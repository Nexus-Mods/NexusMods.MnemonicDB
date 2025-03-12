﻿using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Query;

/// <summary>
/// A slice descriptor for querying datoms, it doesn't contain any data, but can be combined
/// with other objects like databases or indexes to query for datoms.
/// </summary>
public struct SliceDescriptor : ISliceDescriptor
{
    /// <summary>
    /// The lower bound of the slice, inclusive.
    /// </summary>
    public required Datom From { get; init; }

    /// <summary>
    /// The upper bound of the slice, exclusive.
    /// </summary>
    public required Datom To { get; init; }

    /// <summary>
    /// Lazily created history variants of the `from` datom 
    /// </summary>
    private Datom? _fromHistory;
    
    /// <summary>
    /// Lazily created history variants of the `to` datom
    /// </summary>
    private Datom? _toHistory;

    /// <summary>
    /// True if the slice is in reverse order, false otherwise. Reverse order means that a DB query
    /// with this slice will sort the results in descending order.
    /// </summary>
    public bool IsReverse => From.Compare(To) > 0;

    private void CreateHistoryVariants()
    {
        if (_fromHistory is null)
            _fromHistory = From.WithIndex(From.Prefix.Index.HistoryVariant());
        if (_toHistory is null)
            _toHistory = To.WithIndex(To.Prefix.Index.HistoryVariant());
    }

    /// <summary>
    /// Sets the index of both datoms in the slice
    /// </summary>
    public IndexType Index
    {
        get
        {
            if (From.Prefix.Index != To.Prefix.Index)
                throw new InvalidOperationException("From and To datoms must have the same index");
            return From.Prefix.Index;
        }
    }

    /// <summary>
    /// A slice that includes all datoms in the database.
    /// </summary>
    public static readonly SliceDescriptor All = new()
    {
        From = DatomIterators.Datom.Min,
        To = DatomIterators.Datom.Max
    };

    /// <summary>
    /// Return a copy of this slice descriptor with the given index set on each datom.
    /// </summary>
    public SliceDescriptor WithIndex(IndexType index) => new()
    {
        From = From.WithIndex(index),
        To = To.WithIndex(index)
    };

    /// <summary>
    /// Returns this descriptor with a reversed iteration order.
    /// </summary>
    public SliceDescriptor Reversed()
    {
        return new SliceDescriptor
        {
            From = To,
            To = From
        };
    }

    /// <summary>
    /// Returns true if the datom is within the slice, false otherwise.
    /// </summary>
    public bool Includes(in Datom datom)
    {
        return GlobalComparer.Compare(From, datom) <= 0 &&
               GlobalComparer.Compare(datom, To) < 0;
    }

    /// <summary>
    /// Creates a slice descriptor from the to and from datoms
    /// </summary>
    public static SliceDescriptor Create(IndexType index, Datom from, Datom to) => new()
    {
        From = from.WithIndex(index), 
        To = to.WithIndex(index)
    };

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
            To = Datom(EntityId.MaxValueNoPartition, attr, value, TxId.MaxValue, false, attributeCache, index)
        };
    }
    
    /// <summary>
    /// Creates a slice descriptor for the given attribute in the current AVET index for the given range
    /// </summary>
    public static SliceDescriptor Create<THighLevel>(IWritableAttribute<THighLevel> attr, THighLevel fromValue, THighLevel toValue, AttributeCache attributeCache)
    {
        return new SliceDescriptor
        {
            From = Datom(EntityId.MinValueNoPartition, attr, fromValue, TxId.MinValue, false, attributeCache, IndexType.AVETCurrent),
            To = Datom(EntityId.MaxValueNoPartition, attr, toValue, TxId.MaxValue, false, attributeCache, IndexType.AVETCurrent)
        };
    }

    /// <summary>
    /// Creates a slice descriptor for the given reference attribute and entity that is being pointed to.
    /// </summary>
    public static SliceDescriptor Create(ReferenceAttribute attr, EntityId value, AttributeCache attributeCache)
    {
        return new SliceDescriptor
        {
            From = Datom(EntityId.MinValueNoPartition, attr, value, TxId.MinValue, false, attributeCache, IndexType.VAETCurrent),
            To = Datom(EntityId.MaxValueNoPartition, attr, value, TxId.MaxValue, false, attributeCache, IndexType.VAETCurrent)
        };
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
    /// Creates a slice descriptor for the given datoms
    /// </summary>
    public static SliceDescriptor Create(Datom from, Datom to)
    {
        return new SliceDescriptor()
        {
            From = from,
            To = to
        };
    }

    /// <summary>
    /// Creates a slice descriptor for the given attribute from the current AEVT index
    /// reverse lookup.
    /// </summary>
    public static SliceDescriptor Create(AttributeId referenceAttribute, IndexType indexType = IndexType.AEVTCurrent)
    {
        return new SliceDescriptor
        {
            From = Datom(EntityId.MinValueNoPartition, referenceAttribute, TxId.MinValue, false, indexType),
            To = Datom(EntityId.MaxValueNoPartition, referenceAttribute, TxId.MaxValue, false, indexType)
        };
    }


    /// <summary>
    /// Creates a slice descriptor for the given attribute and entity from the EAVT index
    /// </summary>
    public static SliceDescriptor Create(EntityId e, AttributeId a)
    {
        return new SliceDescriptor
        {
            From = Datom(e, a, TxId.MinValue, false, IndexType.EAVTCurrent),
            To = Datom(e, AttributeId.From((ushort)(a.Value + 1)), TxId.MaxValue, false, IndexType.EAVTCurrent)
        };
    }
    
    /// <summary>
    /// Creates a slice descriptor for the given exactly from the given index
    /// </summary>
    public static SliceDescriptor Exact(IndexType index, Datom datom)
    {
        return new SliceDescriptor
        {
            From = datom.WithIndex(index),
            To = datom.WithIndex(index)
        };
    }


    /// <summary>
    /// Creates a slice descriptor for the given attribute in the current AEVT index
    /// </summary>
    public static SliceDescriptor Create(IAttribute attr, AttributeCache attributeCache)
    {
        var attrId = attributeCache.GetAttributeId(attr.Id);
        return new SliceDescriptor
        {
            From = Datom(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false, IndexType.AEVTCurrent),
            To = Datom(EntityId.MaxValueNoPartition, attrId, TxId.MaxValue, false, IndexType.AEVTCurrent)
        };
    }


    /// <summary>
    /// Creates a slice descriptor for datoms that reference the given entity via the VAET index
    /// </summary>
    public static SliceDescriptor CreateReferenceTo(EntityId pointingTo)
    {
        return new SliceDescriptor
        {
            From = Datom(EntityId.MinValueNoPartition, AttributeId.Min, pointingTo, TxId.MinValue, false, IndexType.VAETCurrent),
            To = Datom(EntityId.MaxValueNoPartition, AttributeId.Max, pointingTo, TxId.MaxValue, false, IndexType.VAETCurrent)
        };
    }


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
                From = new Datom(from).WithIndex(index),
                To = new Datom(to).WithIndex(index)
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
                To = new Datom(to).WithIndex(index)
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
            To = Datom(to, AttributeId.Max, TxId.MaxValue, false, IndexType.EAVTCurrent)
        };
    }

    /// <summary>
    /// Creates a datom with no value from the given parts
    /// </summary>
    public static Datom Datom(EntityId e, AttributeId a, EntityId value, TxId id, bool isRetract, IndexType indexType = IndexType.None)
    {
        var data = new Memory<byte>(GC.AllocateUninitializedArray<byte>(KeyPrefix.Size + sizeof(ulong)));
        var span = data.Span;
        var prefix = new KeyPrefix(e, a, id, isRetract, ValueTag.Reference, indexType);
        MemoryMarshal.Write(span, prefix);
        MemoryMarshal.Write(span.SliceFast(KeyPrefix.Size), value);
        return new Datom(data);
    }

    /// <summary>
    /// Creates a slice descriptor for the given entity range. IndexType must be either EAVTCurrent or EAVTHistory
    /// </summary>
    public static SliceDescriptor Create(IndexType indexType, EntityId eid)
    {
        if (indexType is not IndexType.EAVTCurrent and not IndexType.EAVTHistory)
            throw new ArgumentException("IndexType must be EAVTCurrent or EAVTHistory", nameof(indexType));
        
        return new SliceDescriptor
        {
            From = Datom(eid, AttributeId.Min, TxId.MinValue, false, indexType),
            To = Datom(eid, AttributeId.Max, TxId.MaxValue, false, indexType)
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
            To = new Datom(toPrefix, datomValueSpan)
        };
    }

    public void Reset<T>(T iterator, bool useHistory) where T : ILowLevelIterator, allows ref struct
    {
        Datom datomToUse;
        if (useHistory)
        {
            CreateHistoryVariants();
            datomToUse = _fromHistory!.Value;
        }
        else
            datomToUse = From;
        
        iterator.SeekTo(datomToUse.ToArray());
        if (!IsReverse)
            iterator.SeekTo(datomToUse.ToArray());
        else
            iterator.SeekToPrev(datomToUse.ToArray());
    }


    public void MoveNext<T>(T iterator) where T : ILowLevelIterator, allows ref struct
    {
        if (IsReverse)
            iterator.Prev();
        else 
            iterator.Next();
    }

    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool isHistory)
    {
        var datomToUse = From;
        if (isHistory)
        {
            CreateHistoryVariants();
            datomToUse = _toHistory!.Value;
        }
        
        if (IsReverse)
            return GlobalComparer.Compare(keySpan, datomToUse.ToArray()) >= 0;
        else 
            return GlobalComparer.Compare(keySpan, datomToUse.ToArray()) <= 0;
    }

    public void Deconstruct(out Datom from, out Datom to, out bool isReversed)
    {
        from = From;
        to = To;
        isReversed = IsReverse;
    }

    public static AllEntitiesInPartition AllEntities(PartitionId partition) 
    {
        return new AllEntitiesInPartition(partition);
    }
    
    public static AllReverseAttributesInPartition AllReverseAttributesInPartition(PartitionId partition) 
    {
        return new AllReverseAttributesInPartition(partition);
    }
}
