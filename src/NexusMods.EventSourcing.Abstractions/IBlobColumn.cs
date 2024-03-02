using System;
using System.Buffers;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A column that contains an array of blobs.
/// </summary>
public interface IBlobColumn : IEnumerable<ReadOnlyMemory<byte>>
{
    public ReadOnlyMemory<byte> this[int idx] { get; }
    public int Length { get; }

    public IBlobColumn Pack();
    void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>;
}
