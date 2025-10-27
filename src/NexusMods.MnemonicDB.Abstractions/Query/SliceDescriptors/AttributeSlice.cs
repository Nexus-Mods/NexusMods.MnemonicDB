using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A slice descriptor for a specific AttributeId in the AEVT index
/// </summary>
public readonly struct AttributeSlice(AttributeId attrId) : ISliceDescriptor
{
    /// <inheritdoc />
    public void Reset<T>(T iterator, bool useHistory) where T : ILowLevelIterator, allows ref struct
    {
        var index = useHistory ? IndexType.AEVTHistory : IndexType.AEVTCurrent;
        var prefix = new KeyPrefix(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false, ValueTag.Null, index);
        iterator.SeekTo(prefix);
    }

    /// <inheritdoc />
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool useHistory)
    {
        var index = useHistory ? IndexType.AEVTHistory : IndexType.AEVTCurrent;
        var prefix = KeyPrefix.Read(keySpan);
        return prefix.A == attrId && prefix.Index == index;
    }

    /// <inheritdoc />
    public bool IsTotalOrdered => false;

    /// <inheritdoc />
    public void Deconstruct(out Datom fromDatom, out Datom toDatom)
    {
        fromDatom = new Datom(new KeyPrefix(EntityId.MinValueNoPartition, attrId, TxId.MinValue, false, ValueTag.Null, IndexType.AEVTCurrent), Null.Instance);
        toDatom = new Datom(new KeyPrefix(EntityId.MaxValueNoPartition, attrId, TxId.MaxValue, false, ValueTag.Null, IndexType.AEVTCurrent), Null.Instance);
    }
    
    /// <summary>
    /// Uncachable slice.
    /// </summary>
    public object? CacheKey => null;
}
