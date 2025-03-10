using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments.SegmentParts;

public struct ValuePart : ISegmentPart<uint>
{

    
    /// <inheritdoc />
    public static int Size => sizeof(uint);
    public static void Extract(ReadOnlySpan<byte> src, Span<byte> dst, PooledMemoryBufferWriter writer)
    {

    }
}
