using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// A index slice for all datoms in a given index
/// </summary>
/// <param name="attrId"></param>
public readonly struct IndexSlice(IndexType index) : ISliceDescriptor
{
    /// <inheritdoc />
    public void Reset<T>(T iterator, bool useHistory) where T : ILowLevelIterator, allows ref struct
    {
        iterator.SeekTo(new KeyPrefix(index));
    }

    /// <inheritdoc />
    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool useHistory)
    {
        var prefix = KeyPrefix.Read(keySpan);
        return prefix.Index == index;
    }

    /// <inheritdoc />
    public bool IsTotalOrdered => true;
}
