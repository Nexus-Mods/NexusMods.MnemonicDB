using System;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

/// <summary>
/// Readable blob column. This column allows for retrieval of previously stored spans of data.
/// </summary>
public interface IReadable
{

    public int Count { get; }

    /// <summary>
    /// Get the span for teh value at the specified offset.
    /// </summary>
    public ReadOnlySpan<byte> this[int offset] { get; }
}
