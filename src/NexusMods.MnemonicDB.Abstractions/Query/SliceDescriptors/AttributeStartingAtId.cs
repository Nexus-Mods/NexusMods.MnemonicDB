using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A slice descriptor for a specific AttributeId in the AEVT index, starting at the given entity id
/// </summary>
public readonly struct AttributeStartingAtId(AttributeId attrId, EntityId eid) : ISliceDescriptor
{
    /// <inheritdoc />
    public void Reset<T>(T iterator, bool useHistory) where T : ILowLevelIterator, allows ref struct
    {
        var index = useHistory ? IndexType.AEVTHistory : IndexType.AEVTCurrent;
        var prefix = new KeyPrefix(eid, attrId, TxId.MinValue, false, ValueTag.Null, index);
        iterator.SeekTo(prefix);
    }

    /// <inheritdoc />
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool useHistory)
    {
        var index = useHistory ? IndexType.AEVTHistory : IndexType.AEVTCurrent;
        var prefix = KeyPrefix.Read(keySpan);
        return prefix.A == attrId && prefix.Index == index;
    }

    public bool IsTotalOrdered => false;
}
