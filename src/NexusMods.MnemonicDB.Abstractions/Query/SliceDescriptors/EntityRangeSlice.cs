using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A slice that iterates over all datoms for a given entity id range.
/// </summary>
public readonly struct EntityRangeSlice(EntityId fromId, EntityId toId) : ISliceDescriptor
{
    public void Reset<T>(T iterator, bool useHistory) where T : ILowLevelIterator, allows ref struct
    {
        var index = useHistory ? IndexType.EAVTHistory : IndexType.EAVTCurrent;
        var prefix = new KeyPrefix(fromId, AttributeId.Min, TxId.MinValue, false, ValueTag.Null, index);
        iterator.SeekTo(prefix);
    }


    /// <inheritdoc />
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool useHistory)
    {
        var index = useHistory ? IndexType.EAVTHistory : IndexType.EAVTCurrent;
        var prefix = KeyPrefix.Read(keySpan);
        return prefix.E <= toId && prefix.Index == index;
    }

    public bool IsTotalOrdered => true;
    
    public void Deconstruct(out Datom fromDatom, out Datom toDatom)
    {
       fromDatom = new Datom(new KeyPrefix(fromId, AttributeId.Min, TxId.MinValue, false, ValueTag.Null, IndexType.EAVTCurrent), Null.Instance);
       toDatom = new Datom(new KeyPrefix(toId, AttributeId.Max, TxId.MaxValue, false, ValueTag.Null, IndexType.EAVTCurrent), Null.Instance);
    }
}
