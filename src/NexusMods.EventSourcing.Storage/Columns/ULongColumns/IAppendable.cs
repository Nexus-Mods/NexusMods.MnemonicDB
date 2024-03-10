using System;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// A column that can be appended to.
/// </summary>
public interface IAppendable<T>
{
    /// <summary>
    /// Appends the specified value to the column.
    /// </summary>
    /// <param name="value"></param>
    public void Append(T value);

    /// <summary>
    /// Appends the specified values to the column.
    /// </summary>
    /// <param name="values"></param>
    public void Append(ReadOnlySpan<T> values);

    /// <summary>
    /// Appends the specified values to the column.
    /// </summary>
    /// <param name="values"></param>
    public void Append(IEnumerable<T> values);

    /// <summary>
    /// Gets a writeable span of the specified size and increases the length of the column by the size.
    /// </summary>
    public Span<T> GetWritableSpan(int size);
}
