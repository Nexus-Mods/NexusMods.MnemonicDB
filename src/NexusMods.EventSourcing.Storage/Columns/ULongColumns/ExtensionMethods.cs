using System;
using NexusMods.EventSourcing.Abstractions.Columns.ULongColumns;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

public static class ExtensionMethods
{
    private static int MaxStackSize = 128;


    public static IPacked Pack(this IReadable column)
    {

        var tmpSpan = column.Length <= MaxStackSize
            ? stackalloc ulong[column.Length]
            : GC.AllocateUninitializedArray<ulong>(column.Length);

        column.CopyTo(0, tmpSpan);

        var stats = Statistics.Create(tmpSpan);
        return (IPacked)stats.Pack(tmpSpan);

    }

}
