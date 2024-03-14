using System;

namespace NexusMods.EventSourcing.Abstractions.Columns.BlobColumns;

/// <summary>
/// A readable column of ulong values.
/// </summary>
public interface IReadable
{
    /// <summary>
    /// Gets the length of the column in rows.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Get the value at the given index.
    /// </summary>
    public ReadOnlySpan<byte> this[int idx] { get; }


    /// <summary>
    /// Gets the memory of the column, this is a blob that contains the data for the column,
    /// items that are in this area can be found by indexing through Offset and Lengths.
    /// </summary>
    public ReadOnlyMemory<byte> Memory { get; }

    /// <summary>
    /// Gets the lengths of the values in the column, this is likely less efficient than using
    /// other methods and may result in allocations in order to return a projected column. Prefer
    /// other methods if possible.
    /// </summary>
    public ULongColumns.IReadable LengthsColumn { get; }

    /// <summary>
    /// Gets the offsets of the values in the column, this is likely less efficient than using
    /// other methods and may result in allocations in order to return a projected column. Prefer
    /// other methods if possible.
    /// </summary>
    public ULongColumns.IReadable OffsetsColumn { get; }

}
