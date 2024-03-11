using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

/// <summary>
/// Represents a column that exists in an unpacked state and thus
/// can return its contents as a span.
/// </summary>
public interface IUnpacked<T> where T : struct
{
    /// <summary>
    /// Gets a read-only span of the column.
    /// </summary>
    public ReadOnlySpan<T> Span { get; }
}
