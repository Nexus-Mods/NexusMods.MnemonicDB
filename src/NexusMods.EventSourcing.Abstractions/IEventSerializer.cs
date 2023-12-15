using System;

namespace NexusMods.EventSourcing.Abstractions;

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
