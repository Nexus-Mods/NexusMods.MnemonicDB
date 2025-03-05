using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A slice for all reverse attributes in a given partition
/// </summary>
public readonly struct AllReverseAttributesInPartition(PartitionId partitionId) : ISliceDescriptor
{
    /// <inheritdoc />
    public void Reset<T>(T iterator) where T : ILowLevelIterator, allows ref struct
    {
        Span<byte> fullSpan = stackalloc byte[KeyPrefix.Size + sizeof(ulong)];
        var prefix = new KeyPrefix(EntityId.MinValueNoPartition, AttributeId.Min, TxId.MinValue, false, ValueTag.Reference, IndexType.VAETCurrent);
        MemoryMarshal.Write(fullSpan, prefix);
        MemoryMarshal.Write(fullSpan.SliceFast(KeyPrefix.Size), partitionId.MinValue);
        iterator.SeekTo(fullSpan);
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
        if (prefix.Index != IndexType.VAETCurrent)
            return false;
        if (prefix.ValueTag != ValueTag.Reference)
            return false;
        
        var entityId = MemoryMarshal.Read<EntityId>(keySpan.SliceFast(KeyPrefix.Size));
        return entityId.Partition == partitionId;
    }

    /// <inheritdoc />
    public void Deconstruct(out Datom from, out Datom to, out bool isReversed)
    {
        var fromValue = GC.AllocateUninitializedArray<byte>(sizeof(ulong));
        var toValue = GC.AllocateUninitializedArray<byte>(sizeof(ulong));

        MemoryMarshal.Write(fromValue, partitionId.MinValue);
        MemoryMarshal.Write(toValue, partitionId.MaxValue);
        
        from = new Datom(new KeyPrefix(partitionId.MinValue, AttributeId.Min, TxId.MinValue, false, ValueTag.Reference, IndexType.VAETCurrent), fromValue);
        to = new Datom(new KeyPrefix(partitionId.MaxValue, AttributeId.Max, TxId.MaxValue, false, ValueTag.Reference, IndexType.VAETCurrent), toValue);
        isReversed = false;
    }
}
