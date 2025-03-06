using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// Forward slice for a transaction id
/// </summary>
public readonly struct TxIdSlice(TxId txId) : ISliceDescriptor
{
    /// <inheritdoc />
    public void Reset<T>(T iterator) where T : ILowLevelIterator, allows ref struct
    {
        var prefix = new KeyPrefix(EntityId.MinValueNoPartition, AttributeId.Min, txId, false, ValueTag.Null, IndexType.TxLog);
        var spanTo = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in prefix, 1));
        iterator.SeekTo(spanTo);
    }

    /// <inheritdoc />
    public void ResetHistory<T>(T iterator) where T : ILowLevelIterator, allows ref struct
    {
        throw new NotSupportedException("TxIdSlice does not support history");
    }

    /// <inheritdoc />
    public void MoveNext<T>(T iterator) where T : ILowLevelIterator, allows ref struct
    {
        iterator.Next();
    }

    /// <inheritdoc />
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan)
    {
        var prefix = KeyPrefix.Read(keySpan);
        return prefix.T == txId && prefix.Index == IndexType.TxLog;
    }

    /// <inheritdoc />
    public bool ShouldContinueHistory(ReadOnlySpan<byte> keySpan)
    {
        throw new NotSupportedException("TxIdSlice does not support history");
    }

    /// <inheritdoc />
    public void Deconstruct(out Datom from, out Datom to, out bool isReversed)
    {
        from = new Datom(new KeyPrefix(EntityId.MinValueNoPartition, AttributeId.Min, txId, false, ValueTag.Null, IndexType.TxLog), ReadOnlyMemory<byte>.Empty);
        to = new Datom(new KeyPrefix(EntityId.MaxValueNoPartition, AttributeId.Max, txId, false, ValueTag.Null, IndexType.TxLog), ReadOnlyMemory<byte>.Empty);
        isReversed = false;
    }
}
