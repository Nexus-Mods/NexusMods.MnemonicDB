using System;
using System.Buffers;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

public interface ISegmentPart
{ 
    /// <summary>
    /// The size in bytes of the fixed part of the segment.
    /// </summary>
    public static abstract int Size { get; }

    /// <summary>
    /// Extract the segment part from the source datom segment and write it to the destination buffer.
    /// </summary>
    public static abstract void Extract(ReadOnlySpan<byte> src, Span<byte> dst, PooledMemoryBufferWriter writer);
}

public interface ISegmentPart<T> : ISegmentPart
 where T : allows ref struct
{
    
}
