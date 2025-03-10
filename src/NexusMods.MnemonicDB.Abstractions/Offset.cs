using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using Reloaded.Memory.Extensions;
using TransparentValueObjects;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// An offset in an index segment to the start of the value data. This may also include an inline size value,
/// if the value is small enough to be stored inline.
/// </summary>
[ValueObject<uint>]
public readonly partial struct Offset
{
    /// <summary>
    /// Get the span of the value data in the segment
    /// </summary>
    public ReadOnlySpan<byte> GetSpan<TSegment>(in TSegment segment)
        where TSegment : ISegment
    {
        var size = Value & 0xFF;
        var offset = (int)(Value >> 8);

        if (size == 0xFF)
        {
            var span = segment.Data.Span;
            var actualSize = MemoryMarshal.Read<int>(span.SliceFast(offset));
            return segment.Data.Span.SliceFast(offset + sizeof(int), actualSize);
        }
        else
        {
            return segment.Data.Span.SliceFast(offset, (int)size);
        }
        
    }

}
