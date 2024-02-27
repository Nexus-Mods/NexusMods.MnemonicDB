using System;
using System.Buffers;

namespace NexusMods.EventSourcing.Storage.Abstractions;

/// <summary>
/// A column that contains an array of blobs.
/// </summary>
public interface IBlobColumn
{
    public ReadOnlyMemory<byte> this[int idx] { get; }
    public int Length { get; }

    public IBlobColumn Pack();
    void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>;
}
