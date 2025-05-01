using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments.Columns;

public class OffsetColumn : IColumn
{
    public static readonly OffsetColumn Instance = new();
    
    public const uint MaxSegmentOffset = 1 << 24;
    public const uint MaxInlineSize = 0xFF;
    
    public unsafe int FixedSize => sizeof(Offset);
    public Type ValueType => typeof(Offset);
    public void Extract(ReadOnlySpan<byte> src, ReadOnlySpan<byte> valueSpan, Span<byte> dst,
        PooledMemoryBufferWriter writer)
    {
        if (valueSpan.Length > 0)
            throw new NotImplementedException();
        
        var valueSize = src.Length - KeyPrefix.Size;
        var offset = writer.Length;
        Debug.Assert(offset < MaxSegmentOffset);
        
        if (valueSize < MaxInlineSize)
        {
            var finalSize = ((uint)offset << 8) | (uint)valueSize;
            MemoryMarshal.Write(dst, finalSize);
            var outSpan = writer.GetSpan(valueSize);
            src.SliceFast(KeyPrefix.Size).CopyTo(outSpan);
            writer.Advance(valueSize);
        }
        else
        {
            var finalSize = ((uint)offset << 8) | MaxInlineSize;
            MemoryMarshal.Write(dst, finalSize);
            var outSpan = writer.GetSpan(valueSize + sizeof(uint));
            MemoryMarshal.Write(outSpan, valueSize);
            src.SliceFast(KeyPrefix.Size).CopyTo(outSpan.SliceFast(sizeof(uint)));
            writer.Advance(valueSize + sizeof(uint));
        }
    }
}
