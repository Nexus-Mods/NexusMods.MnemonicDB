using System;
using NexusMods.EventSourcing.Abstractions.Columns.BlobColumns;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

public partial class BlobPackedColumn : IReadable
{
    public ReadOnlySpan<byte> this[int offset]
    {
        get
        {
            var start = Offsets[offset];
            var end = Lengths[offset];
            return Data.Slice((int)start, (int)end).Span;
        }
    }

    public ReadOnlyMemory<byte> Memory => Data;

    public IUnpacked Unpack()
    {
        throw new NotImplementedException();
    }

    public ReadOnlyMemory<byte> GetMemory(int offset)
    {
        var start = Offsets[offset];
        var end = Lengths[offset];
        return Data.Slice((int)start, (int)end);
    }
}
