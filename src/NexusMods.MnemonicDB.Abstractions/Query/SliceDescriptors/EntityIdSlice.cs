using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A slice descriptor for a specific EntityId in the EAVT index
/// </summary>
/// <param name="entityId"></param>
public readonly struct EntityIdSlice(EntityId entityId) : ISliceDescriptor
{
    public unsafe void Reset<T>(T iterator) where T : ILowLevelIterator, allows ref struct
    {
        var prefix = new KeyPrefix(entityId, AttributeId.Min, TxId.MinValue, false, ValueTag.Null,
            IndexType.EAVTCurrent);
        var spanTo = MemoryMarshal.AsBytes(MemoryMarshal.GetReference(&prefix, 1));
        iterator.SeekTo(spanTo);
    }

    public void MoveNext<T>(T iterator) where T : ILowLevelIterator, allows ref struct
    {
        iterator.Next();
    }

    public bool ShouldContinue(ReadOnlySpan<byte> keySpan)
    {
        var prefix = KeyPrefix.Read(keySpan);
        return prefix.E == entityId && prefix.Index == IndexType.EAVTCurrent;
    }
}
