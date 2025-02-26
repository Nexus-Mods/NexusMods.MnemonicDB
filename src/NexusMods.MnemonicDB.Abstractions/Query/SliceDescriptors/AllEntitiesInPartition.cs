using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A slice descriptor for all the entities in a partition via the EAVT Index.
/// </summary>
public struct AllEntitiesInPartition(PartitionId partitionId) : ISliceDescriptor
{
    /// <inheritdoc />
    public void Reset<T>(T iterator) where T : ILowLevelIterator, allows ref struct
    {
        var prefix = new KeyPrefix(partitionId.MinValue, AttributeId.Min, TxId.MinValue, false, ValueTag.Null, IndexType.EAVTCurrent);
        var spanTo = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in prefix, 1));
        iterator.SeekTo(spanTo);
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
        return prefix.Index == IndexType.EAVTCurrent && prefix.E.Partition == partitionId;
    }

    /// <inheritdoc />
    public void Deconstruct(out Datom from, out Datom to, out bool isReversed)
    {
        from = new Datom(new KeyPrefix(partitionId.MinValue, AttributeId.Min, TxId.MinValue, false, ValueTag.Null, IndexType.EAVTCurrent), ReadOnlyMemory<byte>.Empty);
        to = new Datom(new KeyPrefix(partitionId.MaxValue, AttributeId.Max, TxId.MaxValue, false, ValueTag.Null, IndexType.EAVTCurrent), ReadOnlyMemory<byte>.Empty);
        isReversed = false;
    }
}
