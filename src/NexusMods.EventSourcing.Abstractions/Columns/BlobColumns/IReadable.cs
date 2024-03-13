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

    public IUnpacked Unpack();
}
