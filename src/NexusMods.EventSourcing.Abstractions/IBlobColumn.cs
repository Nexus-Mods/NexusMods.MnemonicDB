using System;
using System.Buffers;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A column that contains an array of blobs.
/// </summary>
public interface IBlobColumn : IEnumerable<ReadOnlyMemory<byte>>
{
    /// <summary>
    /// Get the blob at the given index.
    /// </summary>
    public ReadOnlyMemory<byte> this[int idx] { get; }

    /// <summary>
    /// The number of blobs in the column.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Packs the column into a lightweight compressed form.
    /// </summary>
    /// <returns></returns>
    public IBlobColumn Pack();

    /// <summary>
    /// Writes the column to the given writer.
    /// </summary>
    void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>;
}
