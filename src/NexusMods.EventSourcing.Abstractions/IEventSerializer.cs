using System;
using System.Buffers;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A serializer for events, is typed to a specific writer type to reduce virtual calls internally
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serializes the given event into span, the span is valid until the next call to this method.
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public ReadOnlySpan<byte> Serialize(IEvent @event);

    /// <summary>
    /// Deserializes the given span into an event
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public IEvent Deserialize(ReadOnlySpan<byte> data);
}
