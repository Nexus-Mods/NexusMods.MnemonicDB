using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A index slice for all datoms in the database
/// </summary>
/// <param name="attrId"></param>
public readonly struct AllSlice : ISliceDescriptor
{
    /// <inheritdoc />
    public void Reset<T>(T iterator, bool useHistory) where T : ILowLevelIterator, allows ref struct
    {
        iterator.SeekTo(new KeyPrefix(EntityId.MaxValueNoPartition, AttributeId.Min, TxId.MinValue, false, ValueTag.Null, 0));
    }

    /// <inheritdoc />
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool useHistory)
    {
        return true;
    }

    /// <inheritdoc />
    public bool IsTotalOrdered => true;

    public void Deconstruct(out Datom fromDatom, out Datom toDatom)
    {
        fromDatom = new Datom(new KeyPrefix(EntityId.MinValueNoPartition, AttributeId.Min, TxId.MinValue, false, ValueTag.Null, IndexType.TxLog), Null.Instance);
        toDatom = new Datom(new KeyPrefix(EntityId.MaxValueNoPartition, AttributeId.Max, TxId.MaxValue, false, ValueTag.Null, IndexType.AVETHistory), Null.Instance);
    }
}
