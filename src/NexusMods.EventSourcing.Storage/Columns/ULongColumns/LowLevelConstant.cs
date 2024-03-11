using System;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

public struct LowLevelConstant
{
    public ulong Value;
    public int Length;

    public ulong Get(ReadOnlySpan<byte> span, int idx)
    {
        return Value;
    }

    public void CopyTo(int offset, Span<ulong> dest)
    {
        throw new NotImplementedException();
    }
}
