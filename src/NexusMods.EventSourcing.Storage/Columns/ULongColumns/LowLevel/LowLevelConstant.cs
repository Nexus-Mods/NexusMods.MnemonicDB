using System;
using System.Runtime.CompilerServices;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns.LowLevel;

public struct LowLevelConstant
{
    public ulong Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Get()
    {
        return Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<ulong> dest)
    {
        dest.Fill(Value);
    }
}
