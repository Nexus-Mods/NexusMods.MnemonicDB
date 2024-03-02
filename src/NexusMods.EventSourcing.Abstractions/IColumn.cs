using System;
using System.Buffers;

namespace NexusMods.EventSourcing.Abstractions;

public interface IColumn<T>
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

    /// <summary>
    /// Packs the column into a more efficient representation.
    /// </summary>
    /// <returns></returns>
    public IColumn<T> Pack();

    /// <summary>
    /// Writes the column to the specified writer.
    /// </summary>
    /// <param name="writer"></param>
    /// <typeparam name="TWriter"></typeparam>
    void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>;

    /// <summary>
    /// Copies the column to the specified destination.
    /// </summary>
    /// <param name="destination"></param>
    void CopyTo(Span<T> destination);
}
