using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments.SegmentParts;

/// <summary>
/// A segment part that extracts the value type from a datom
/// </summary>
public struct ValueTypePart : ISegmentPart<ValueTag>
{
    /// <inheritdoc />
    public static int Size => sizeof(byte);

    /// <inheritdoc />
    public static void Extract(ReadOnlySpan<byte> src, Span<byte> dst, PooledMemoryBufferWriter writer)
    {
        dst[0] = (byte)KeyPrefix.Read(src).ValueTag;
    }
}
