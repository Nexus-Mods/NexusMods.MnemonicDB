using System;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

public unsafe struct LowLevelUnpacked
{
    public ulong Get(ReadOnlySpan<byte> span, int idx)
    {
        return MemoryMarshal.Read<ulong>(span.SliceFast(idx * sizeof(ulong), sizeof(ulong)));
    }

    public void CopyTo(int offset, Span<ulong> dest)
    {
        throw new NotImplementedException();
    }
}
