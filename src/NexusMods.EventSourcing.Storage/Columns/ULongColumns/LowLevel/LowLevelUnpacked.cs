using System;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns.LowLevel;

public unsafe struct LowLevelUnpacked
{
    public ulong Get(ReadOnlySpan<byte> span, int idx)
    {
        return MemoryMarshal.Read<ulong>(span.SliceFast(idx * sizeof(ulong), sizeof(ulong)));
    }

    public ulong Get(byte* span, int idx)
    {
        return *(ulong *)(span + idx * sizeof(ulong));
    }

    public void CopyTo(ReadOnlySpan<byte> src, int offset, Span<ulong> dest)
    {
        src.Cast<byte, ulong>().SliceFast(offset, dest.Length).CopyTo(dest);
    }

    public void CopyTo(byte* src, int offset, Span<ulong> dest)
    {
        var srcSpan = new ReadOnlySpan<byte>(src, dest.Length * sizeof(ulong));
        srcSpan.Cast<byte, ulong>().SliceFast(offset, dest.Length).CopyTo(dest);
    }
}
