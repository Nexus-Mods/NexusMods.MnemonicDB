using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Query;

/// <summary>
/// A slice descriptor for querying datoms, it doesn't contain any data, but can be combined
/// with other objects like databases or indexes to query for datoms.
/// </summary>
public readonly struct SliceDescriptor
{
    /// <summary>
    /// The index to query, the `From` and `To` should be within the same index.
    /// </summary>
    public required IndexType Index { get; init; }

    /// <summary>
    /// The lower bound of the slice, inclusive.
    /// </summary>
    public required Datom From { get; init; }

    /// <summary>
    /// The upper bound of the slice, exclusive.
    /// </summary>
    public required Datom To { get; init; }

    /// <summary>
    /// True if the slice is in reverse order, false otherwise. Reverse order means that a DB query
    /// with this slice will sort the results in descending order.
    /// </summary>
    public bool IsReverse => From.Compare(To, Index) > 0;

    /// <summary>
    /// Returns true if the datom is within the slice, false otherwise.
    /// </summary>
    public bool Includes(in Datom datom)
    {
        return Index switch
        {
            IndexType.TxLog => DatomComparators.TxLogComparator.Compare(From, datom) <= 0 &&
                               DatomComparators.TxLogComparator.Compare(datom, To) < 0,
            IndexType.EAVTCurrent or IndexType.EAVTHistory =>
                DatomComparators.EAVTComparator.Compare(From, datom) <= 0 &&
                DatomComparators.EAVTComparator.Compare(datom, To) < 0,
            IndexType.AEVTCurrent or IndexType.AEVTHistory =>
                DatomComparators.AEVTComparator.Compare(From, datom) <= 0 &&
                DatomComparators.AEVTComparator.Compare(datom, To) < 0,
            IndexType.VAETCurrent or IndexType.VAETHistory =>
                DatomComparators.VAETComparator.Compare(From, datom) <= 0 &&
                DatomComparators.VAETComparator.Compare(datom, To) < 0,
            _ => throw new ArgumentOutOfRangeException(nameof(Index), Index, "Unknown index type")
        };
    }

    /// <summary>
    /// Creates a slice descriptor from the to and from datoms
    /// </summary>
    public static SliceDescriptor Create(IndexType index, Datom from, Datom to) => new() { Index = index, From = from, To = to };

    /// <summary>
    /// Creates a slice descriptor for the given entity in the current EAVT index
    /// </summary>
    public static SliceDescriptor Create(EntityId e, IAttributeRegistry registry)
    {
        return new SliceDescriptor
        {
            Index = IndexType.EAVTCurrent,
            From = Datom(e, AttributeId.Min, TxId.MinValue, false, registry),
            To = Datom(e, AttributeId.Max, TxId.MaxValue, false, registry)
        };
    }

    /// <summary>
    /// Creates a slice descriptor for the given transaction in the TxLog index
    /// </summary>
    public static SliceDescriptor Create(TxId tx, IAttributeRegistry registry)
    {
        return new SliceDescriptor
        {
            Index = IndexType.TxLog,
            From = Datom(EntityId.MinValueNoPartition, AttributeId.Min, tx, false, registry),
            To = Datom(EntityId.MaxValueNoPartition, AttributeId.Max, tx, false, registry)
        };
    }

    /// <summary>
    /// Creates a slice descriptor for the given attribute in the current AVET index
    /// </summary>
    public static SliceDescriptor Create<THighLevel, TLowLevel>(Attribute<THighLevel, TLowLevel> attr, THighLevel value, IAttributeRegistry registry)
    {
        return new SliceDescriptor
        {
            Index = IndexType.AVETCurrent,
            From = Datom(EntityId.MinValueNoPartition, attr, value, TxId.MinValue, false, registry),
            To = Datom(EntityId.MaxValueNoPartition, attr, value, TxId.MaxValue, false, registry)
        };
    }

    /// <summary>
    /// Creates a slice descriptor for the given reference attribute and entity that is being pointed to.
    /// </summary>
    public static SliceDescriptor Create(ReferenceAttribute attr, EntityId value, IAttributeRegistry registry)
    {
        return new SliceDescriptor
        {
            Index = IndexType.VAETCurrent,
            From = Datom(EntityId.MinValueNoPartition, attr, value, TxId.MinValue, false, registry),
            To = Datom(EntityId.MaxValueNoPartition, attr, value, TxId.MaxValue, false, registry)
        };
    }

    /// <summary>
    /// Creates a slice descriptor for the given reference attribute and entity that is being pointed to, this is a
    /// reverse lookup.
    /// </summary>
    public static SliceDescriptor Create(AttributeId referenceAttribute, EntityId pointingTo, IAttributeRegistry dbRegistry)
    {
        return new SliceDescriptor
        {
            Index = IndexType.VAETCurrent,
            From = Datom(EntityId.MinValueNoPartition, referenceAttribute, pointingTo, TxId.MinValue, false, dbRegistry),
            To = Datom(EntityId.MaxValueNoPartition, referenceAttribute, pointingTo, TxId.MaxValue, false, dbRegistry)
        };
    }

    /// <summary>
    /// Creates a slice descriptor for the given attribute from the current AEVT index
    /// reverse lookup.
    /// </summary>
    public static SliceDescriptor Create(AttributeId referenceAttribute, IAttributeRegistry dbRegistry)
    {
        return new SliceDescriptor
        {
            Index = IndexType.AEVTCurrent,
            From = Datom(EntityId.MinValueNoPartition, referenceAttribute, TxId.MinValue, false, dbRegistry),
            To = Datom(EntityId.MaxValueNoPartition, referenceAttribute, TxId.MaxValue, false, dbRegistry)
        };
    }


    /// <summary>
    /// Creates a slice descriptor for the given attribute and entity from the EAVT index
    /// </summary>
    public static SliceDescriptor Create(EntityId e, AttributeId a, IAttributeRegistry dbRegistry)
    {
        return new SliceDescriptor
        {
            Index = IndexType.EAVTCurrent,
            From = Datom(e, a, TxId.MinValue, false, dbRegistry),
            To = Datom(e, AttributeId.From((ushort)(a.Value + 1)), TxId.MaxValue, false, dbRegistry)
        };
    }

    /// <summary>
    /// Creates a slice descriptor that points only to the specific attribute
    /// </summary>
    public static SliceDescriptor Create(IndexType index, ReadOnlySpan<byte> span, IAttributeRegistry registry)
    {
        var array = span.ToArray();
        return new SliceDescriptor
        {
            Index = index,
            From = new Datom(array, registry),
            To = new Datom(array, registry)
        };
    }

    /// <summary>
    /// Creates a slice descriptor for the given exactly from the given index
    /// </summary>
    public static SliceDescriptor Exact(IndexType index, ReadOnlySpan<byte> span, IAttributeRegistry registry)
    {
        var from = span.ToArray();
        var to = span.ToArray();
        return new SliceDescriptor
        {
            Index = index,
            From = new Datom(from, registry),
            To = new Datom(to, registry)
        };
    }


    /// <summary>
    /// Creates a slice descriptor for the given attribute in the current AEVT index
    /// </summary>
    public static SliceDescriptor Create(IAttribute attr, IAttributeRegistry registry)
    {
        var attrId = attr.GetDbId(registry.Id);
        return new SliceDescriptor
        {
            Index = IndexType.AEVTCurrent,
            From = Datom(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false, registry),
            To = Datom(EntityId.MaxValueNoPartition, attrId, TxId.MaxValue, false, registry)
        };
    }


    /// <summary>
    /// Creates a slice descriptor for datoms that reference the given entity via the VAET index
    /// </summary>
    public static SliceDescriptor CreateReferenceTo(EntityId pointingTo, IAttributeRegistry dbRegistry)
    {
        return new SliceDescriptor
        {
            Index = IndexType.VAETCurrent,
            From = Datom(EntityId.MinValueNoPartition, AttributeId.Min, pointingTo, TxId.MinValue, false, dbRegistry),
            To = Datom(EntityId.MaxValueNoPartition, AttributeId.Max, pointingTo, TxId.MaxValue, false, dbRegistry)
        };
    }


    /// <summary>
    /// Creates a slice descriptor for the entire index
    /// </summary>
    public static SliceDescriptor Create(IndexType index, IAttributeRegistry registry)
    {
        if (index is IndexType.VAETCurrent or IndexType.VAETHistory)
        {
            // VAET has a special case where we need to include the reference type and an actual reference
            // in the slice
            var from = GC.AllocateUninitializedArray<byte>(KeyPrefix.Size + sizeof(ulong));
            from.AsSpan().Clear();

            var fromPrefix = new KeyPrefix(EntityId.MinValueNoPartition, AttributeId.Min, TxId.MinValue, false, ValueTags.Reference);
            MemoryMarshal.Write(from, fromPrefix);


            var to = GC.AllocateUninitializedArray<byte>(KeyPrefix.Size + sizeof(ulong));
            to.AsSpan().Fill(byte.MaxValue);

            var toPrefix = new KeyPrefix(EntityId.MaxValueNoPartition, AttributeId.Max, TxId.MaxValue, true, ValueTags.Reference);
            MemoryMarshal.Write(to, toPrefix);

            return new SliceDescriptor
            {
                Index = index,
                From = new Datom(from, registry),
                To = new Datom(to, registry)
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
                Index = index,
                From = new Datom(from, registry),
                To = new Datom(to, registry)
            };
        }

    }

    /// <summary>
    /// Creates a datom with no value from the given parts
    /// </summary>
    public static Datom Datom(EntityId e, AttributeId a, TxId id, bool isRetract, IAttributeRegistry registry)
    {
        var data = GC.AllocateUninitializedArray<byte>(KeyPrefix.Size);
        var prefix = new KeyPrefix(e, a, id, isRetract, ValueTags.Null);
        MemoryMarshal.Write(data, prefix);
        return new Datom(data, registry);
    }

    /// <summary>
    /// Creates a with a value from the given attribute and value
    /// </summary>
    public static Datom Datom<THighLevel, TLowLevel>(EntityId e, Attribute<THighLevel, TLowLevel> a, THighLevel value, TxId tx, bool isRetract, IAttributeRegistry registry)
    {
        using var pooled = new PooledMemoryBufferWriter();
        a.Write(e, registry.Id, value, tx, isRetract, pooled);
        return new Datom(pooled.WrittenMemory.ToArray(), registry);
    }

    /// <summary>
    /// Creates a slice descriptor for the given entity range, for the current EAVT index
    /// </summary>
    public static SliceDescriptor Create(EntityId from, EntityId to, IAttributeRegistry dbRegistry)
    {
        return new SliceDescriptor
        {
            Index = IndexType.EAVTCurrent,
            From = Datom(from, AttributeId.Min, TxId.MinValue, false, dbRegistry),
            To = Datom(to, AttributeId.Max, TxId.MaxValue, false, dbRegistry)
        };
    }

    /// <summary>
    /// Creates a datom with no value from the given parts
    /// </summary>
    public static Datom Datom(EntityId e, AttributeId a, EntityId value, TxId id, bool isRetract, IAttributeRegistry registry)
    {
        throw new NotImplementedException();
        var data = new Memory<byte>(GC.AllocateUninitializedArray<byte>(KeyPrefix.Size + sizeof(ulong)));
        var span = data.Span;
        var prefix = new KeyPrefix(e, a, id, isRetract, ValueTags.Null);
        MemoryMarshal.Write(span, prefix);
        MemoryMarshal.Write(span.SliceFast(KeyPrefix.Size), value);
        return new Datom(data, registry);
    }

}
