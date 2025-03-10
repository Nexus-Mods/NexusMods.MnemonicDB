using System;
using System.Runtime.CompilerServices;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments.Columns;

public static class ColumnDefinitions
{
    /// <summary>
    /// Get the column instance for the specified type
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static IColumn ColumnFor<T>()
    {
        if (typeof(T) == typeof(EntityId))
            return EntityIdColumn.Instance;
        else if (typeof(T) == typeof(AttributeId))
            return AttributeIdColumn.Instance;
        else if (typeof(T) == typeof(Offset))
            return OffsetColumn.Instance;
        else
            throw new NotSupportedException($"Type {typeof(T)} is not supported.");
    }
}
