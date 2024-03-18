using System;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions.Columns;

/// <summary>
/// Represents a column of scalar values, such as integers, longs, or floats. Values can be added to the column,
/// and be indexed and read from the column. The column can later be "frozen" which will optimize the internal structure
/// for long-term storage. Once the collection is frozen, it is read-only, and future appends will create a new column.
/// </summary>
public interface IScalarColumn<T> where T : unmanaged
{
    /// <summary>
    /// If the column is frozen, it is read-only, and future appends will create
    /// a new column instance
    /// </summary>
    public bool IsFrozen { get; }

    /// <summary>
    /// Freezes the column, making it read-only.
    /// </summary>
    /// <returns></returns>
    public bool Freeze();

    /// <summary>
    /// Gets the value at the given index.
    /// </summary>
    public T this[int idx] { get; }

    /// <summary>
    /// Gets the length of the column.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Sets the value at the given index.
    /// </summary>
    public IScalarColumn<T> Set(int idx, T value);

    /// <summary>
    /// Adds a value to the column.
    /// </summary>
    public IScalarColumn<T> Add(T value);

    /// <summary>
    /// Adds a collection of values to the column.
    /// </summary>
    public IScalarColumn<T> Add(ReadOnlySpan<T> values);

    /// <summary>
    /// Adds a collection of values to the column.
    /// </summary>
    public IScalarColumn<T> Add(IEnumerable<T> values);

    /// <summary>
    /// Adds a collection of values to the column.
    /// </summary>
    public IScalarColumn<T> Add(params T[] values);

    /// <summary>
    /// Copies the column to the given destination starting at the given offset.
    /// </summary>
    public void CopyTo(int offset, Span<T> dest);
}
