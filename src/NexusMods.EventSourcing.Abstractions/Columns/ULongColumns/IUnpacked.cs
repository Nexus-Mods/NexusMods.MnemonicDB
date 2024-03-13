using System;

namespace NexusMods.EventSourcing.Abstractions.Columns.ULongColumns;

/// <summary>
/// Represents a column that exists in an unpacked state and thus
/// can return its contents as a span.
/// </summary>
public partial interface IUnpacked
{
    /// <summary>
    /// Gets a read-only span of the column.
    /// </summary>
    public ReadOnlySpan<ulong> Span { get; }

    public IReadable Pack();
}
