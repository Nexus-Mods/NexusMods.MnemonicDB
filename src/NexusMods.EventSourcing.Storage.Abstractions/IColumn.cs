using System;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IColumn<out T>
{
    /// <summary>
    /// Gets the item at the specified index, this may be slow depending on the
    /// encoding of the data.
    /// </summary>
    /// <param name="index"></param>
    public T this[int index] { get; }

    /// <summary>
    /// Gets the length of the column in rows.
    /// </summary>
    public int Length { get; }
}
