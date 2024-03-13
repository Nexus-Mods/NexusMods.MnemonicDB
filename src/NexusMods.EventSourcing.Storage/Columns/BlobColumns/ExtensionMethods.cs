using NexusMods.EventSourcing.Abstractions.Columns.BlobColumns;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

public static class ExtensionMethods
{
    public static IReadable Pack(this IReadable column)
    {
        return new BlobPackedColumn
        {
            Count = column.Count,
            Lengths = (ULongPackedColumn)column.Lengths.Pack(),
            Offsets = (ULongPackedColumn)column.Offsets.Pack(),
            Data = column.Memory,
        };
    }

}
