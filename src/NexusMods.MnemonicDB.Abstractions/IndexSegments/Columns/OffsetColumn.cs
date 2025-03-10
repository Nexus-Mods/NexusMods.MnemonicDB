using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments.Columns;

public class OffsetColumn : IColumn<Offset>
{
    public static readonly OffsetColumn Instance = new();
    
    public const uint MaxSegmentOffset = 1 << 24;
    public const uint MaxInlineSize = 0xFF;
    public const uint OversizeMask = 0xFF000000;
    
    public unsafe int FixedSize => sizeof(Offset);
    public Type ValueType => typeof(Offset);
    public void Extract(ReadOnlySpan<byte> src, Span<byte> dst, PooledMemoryBufferWriter writer)
    {
        var valueSize = src.Length - KeyPrefix.Size;
        var offset = writer.Length;
        Debug.Assert(offset < MaxSegmentOffset);
        
        if (valueSize < MaxInlineSize)
        {
            var finalSize = (offset << 8) | valueSize;
            MemoryMarshal.Write(dst, finalSize);
            var outSpan = writer.GetSpan(valueSize);
            src.SliceFast(KeyPrefix.Size).CopyTo(outSpan);
            writer.Advance(valueSize);
        }
        else
        {
            var finalSize = (offset << 8) | MaxInlineSize;
            MemoryMarshal.Write(dst, finalSize);
            var outSpan = writer.GetSpan(valueSize + sizeof(int));
            MemoryMarshal.Write(outSpan, offset);
            src.SliceFast(KeyPrefix.Size).CopyTo(outSpan.SliceFast(sizeof(int)));
            writer.Advance(valueSize + sizeof(int));
        }
    }
}
