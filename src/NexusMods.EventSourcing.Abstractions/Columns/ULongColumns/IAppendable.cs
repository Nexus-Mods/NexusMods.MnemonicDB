using System;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// A column that can be appended to.
/// </summary>
public interface IAppendable
{
    /// <summary>
    /// Appends the specified value to the column.
    /// </summary>
    /// <param name="value"></param>
    public void Append(ulong value);

    /// <summary>
    /// Appends the specified values to the column.
    /// </summary>
    /// <param name="values"></param>
    public void Append(ReadOnlySpan<ulong> values);

    /// <summary>
    /// Appends the specified values to the column using the specified mask as a filter, assumes that the mask
    /// is the size of DatomChunk.ChunkSize / 64.
    /// </summary>
    /// <param name="values"></param>
    /// <param name="mask"></param>
    public void Append(ReadOnlySpan<ulong> values, ReadOnlySpan<ulong> mask);

    /// <summary>
    /// Appends the specified values to the column.
    /// </summary>
    /// <param name="values"></param>
    public void Append(IEnumerable<ulong> values);

    /// <summary>
    /// Gets a writeable span of the specified size and increases the length of the column by the size.
    /// </summary>
    public Span<ulong> GetWritableSpan(int size);

    /// <summary>
    /// Sets the length of the column, any values in the expanded area are undefined.
    /// </summary>
    /// <param name="length"></param>
    public void SetLength(int length);

    /// <summary>
    /// Sets the value at the specified index.
    /// </summary>
    /// <param name="idx"></param>
    public ulong this[int idx] { set; }
}
