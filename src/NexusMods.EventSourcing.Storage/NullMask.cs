using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.EventSourcing.Storage.Abstractions;

/// <summary>
/// Null masks are used in many places to indicate what parts of a chunk are null or should be ignored.
/// </summary>
public unsafe struct NullMask
{
    public fixed ulong Mask[RawDataChunk.DefaultChunkSize / 64];

    /// <summary>
    /// Sets or gets the state of the mask at the given offset.
    /// </summary>
    /// <param name="offset"></param>
    public bool this[int offset]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var maskIdx = offset / 64;
            var bitIdx = offset % 64;
            return (Mask[maskIdx] & (1UL << bitIdx)) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            var maskIdx = offset / 64;
            var bitIdx = offset % 64;
            if (value)
                Mask[maskIdx] |= 1UL << bitIdx;
            else
                Mask[maskIdx] &= ~(1UL << bitIdx);
        }
    }

    public void Clear()
    {
        fixed(ulong* mask = Mask)
        {
            var span = new Span<ulong>(mask, RawDataChunk.DefaultChunkSize / 64);
            span.Clear();
        }
    }

    /// <summary>
    /// Sets everything in the mask 0 after the given length and 1 before it.
    /// </summary>
    /// <param name="length"></param>
    public void SetLength(int length)
    {
        fixed(ulong* mask = Mask)
        {
            var span = new Span<ulong>(mask, RawDataChunk.DefaultChunkSize / 64);
            span.Fill(0);
            span.Slice(0, length / 64).Fill(ulong.MaxValue);
        }
    }

}
