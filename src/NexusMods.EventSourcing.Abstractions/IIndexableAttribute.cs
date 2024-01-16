using System.Buffers;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Marker for attribute types that can be indexed.
/// </summary>
public interface IIndexableAttribute
{
}


/// <summary>
/// Typed interface for indexed attributes.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public interface IIndexableAttribute<TValue>
{
    /// <summary>
    /// Writes the value to the writer, should be written in the same format as the accumulator for this attribute
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <typeparam name="TWriter"></typeparam>
    public void Write<TWriter>(TWriter writer, TValue value) where TWriter : IBufferWriter<byte>;
}
