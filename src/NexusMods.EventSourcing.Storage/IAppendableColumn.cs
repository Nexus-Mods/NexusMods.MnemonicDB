using System;

namespace NexusMods.EventSourcing.Storage.Abstractions;

/// <summary>
/// A column that can have data appended to it.
/// </summary>
public interface IAppendableColumn<in T>
{
    /// <summary>
    /// Appends a value to the column.
    /// </summary>
    /// <param name="value"></param>
    public void Append(T value);

    /// <summary>
    /// Swaps the values at the given indices.
    /// </summary>
    /// <param name="idx1"></param>
    /// <param name="idx2"></param>
    public void Swap(int idx1, int idx2);
}
