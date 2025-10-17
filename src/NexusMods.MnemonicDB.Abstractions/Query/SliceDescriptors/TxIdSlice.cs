using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// Forward slice for a transaction id
/// </summary>
public readonly struct TxIdSlice(TxId txId) : ISliceDescriptor
{
    /// <inheritdoc />
    public void Reset<T>(T iterator, bool useHistory) where T : ILowLevelIterator, allows ref struct
    {
        // There is no history for a TxLog, so we do nothing.
        if (useHistory)
            return;
        var prefix = new KeyPrefix(EntityId.MinValueNoPartition, AttributeId.Min, txId, false, ValueTag.Null, IndexType.TxLog);
        iterator.SeekTo(prefix);
    }
    
    /// <inheritdoc />
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool useHistory)
    {
        if (useHistory)
            return false;
        var prefix = KeyPrefix.Read(keySpan);
        return prefix.T == txId && prefix.Index == IndexType.TxLog;
    }

    public bool IsTotalOrdered => false;
}
