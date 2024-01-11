using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// An accumulator is used to accumulate values from events, it is opaque to everything but the attribute definition that created it.
/// </summary>
public interface IAccumulator
{

    /// <summary>
    /// Writes the accumulator to the given buffer writer.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="registry"></param>
    public void WriteTo(IBufferWriter<byte> writer, ISerializationRegistry registry);

    /// <summary>
    /// Reads the accumulator from the given buffer reader, the span may be larger than the accumulator. Returns the
    /// number of bytes read.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="registry"></param>
    public int ReadFrom(ref ReadOnlySpan<byte> reader, ISerializationRegistry registry);
}
