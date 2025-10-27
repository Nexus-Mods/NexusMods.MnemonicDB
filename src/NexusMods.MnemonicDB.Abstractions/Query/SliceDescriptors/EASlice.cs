using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A slice that selects the datoms for a given entity and attribute.
/// </summary>
// ReSharper disable once InconsistentNaming
public readonly struct EASlice(EntityId entityId, AttributeId attrId) : ISliceDescriptor
{
    /// <inheritdoc />
    public void Reset<T>(T iterator, bool useHistory) where T : ILowLevelIterator, allows ref struct
    {
        var index = useHistory ? IndexType.EAVTHistory : IndexType.EAVTCurrent;
        var prefix = new KeyPrefix(entityId, attrId, TxId.MinValue, false, ValueTag.Null, index);
        iterator.SeekTo(prefix);
    }

    /// <inheritdoc />
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool useHistory)
    {
        var index = useHistory ? IndexType.EAVTHistory : IndexType.EAVTCurrent;
        var prefix = KeyPrefix.Read(keySpan);
        return prefix.A == attrId && prefix.E == entityId && prefix.Index == index;
    }

    public bool IsTotalOrdered => false;
    
    public void Deconstruct(out Datom fromDatom, out Datom toDatom)
    {
        fromDatom = new Datom(new KeyPrefix(entityId, attrId, TxId.MinValue, false, ValueTag.Null, IndexType.EAVTCurrent), Null.Instance);
        toDatom = new Datom(new KeyPrefix(entityId, attrId, TxId.MaxValue, false, ValueTag.Null, IndexType.EAVTCurrent), Null.Instance);
    }
    
    /// <summary>
    /// Uncachable slice.
    /// </summary>
    public object? CacheKey => null;
}
