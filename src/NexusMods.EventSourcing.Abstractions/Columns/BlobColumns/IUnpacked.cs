using System;

namespace NexusMods.EventSourcing.Abstractions.Columns.BlobColumns;

/// <summary>
/// A unpacked blob column. This column
/// </summary>
public interface IUnpacked : IReadable
{

    /// <summary>
    /// Get the data span for the column.
    /// </summary>
    public ReadOnlySpan<byte> Span { get; }

    /// <summary>
    /// A span of offsets into the column for each value.
    /// </summary>
    public ULongColumns.IUnpacked Offsets { get; }

    /// <summary>
    /// A span of lengths for each value in the column.
    /// </summary>
    public ULongColumns.IUnpacked Lengths { get; }

    public IReadable Pack();
}
