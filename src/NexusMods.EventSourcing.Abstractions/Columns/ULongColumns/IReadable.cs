using System;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions.Columns.ULongColumns;

/// <summary>
/// A readable column of ulong values.
/// </summary>
public interface IReadable : IEnumerable<ulong>
{
    /// <summary>
    /// Gets the length of the column in rows.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Copies the column to the specified destination offset by the specified offset.
    /// </summary>
    public void CopyTo(int offset, Span<ulong> dest);


    /// <summary>
    /// Get the value at the given index.
    /// </summary>
    public ulong this[int idx] { get; }
}
