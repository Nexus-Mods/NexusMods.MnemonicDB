using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A slice descriptor for all the entities in a partition via the EAVT Index.
/// </summary>
public readonly struct AllEntitiesInPartition(PartitionId partitionId) : ISliceDescriptor
{
    /// <inheritdoc />
    public void Reset<T>(T iterator, bool history = false) where T : ILowLevelIterator, allows ref struct
    {
        var index = history ? IndexType.EAVTHistory : IndexType.EAVTCurrent;
        var prefix = new KeyPrefix(partitionId.MinValue, AttributeId.Min, TxId.MinValue, false, ValueTag.Null, index);
        iterator.SeekTo(prefix);
    }
    
    /// <inheritdoc />
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool history = false)
    {
        var index = history ? IndexType.EAVTHistory : IndexType.EAVTCurrent;
        var prefix = KeyPrefix.Read(keySpan);
        return prefix.Index == index && prefix.E.Partition == partitionId;
    }

    public bool IsTotalOrdered => true;
}
