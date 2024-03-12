using System;

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
}
