using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments.SegmentParts;

/// <inheritdoc />
public struct AttributeIdPart : ISegmentPart<AttributeId>
{
    /// <inheritdoc />
    public static unsafe int Size => sizeof(AttributeId);

    /// <inheritdoc />
    public static void Extract(ReadOnlySpan<byte> src, Span<byte> dst, PooledMemoryBufferWriter writer)
    {
        var prefix = KeyPrefix.Read(src);
        MemoryMarshal.Write(dst, prefix.A);
    }
}
