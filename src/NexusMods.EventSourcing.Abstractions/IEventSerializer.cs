namespace NexusMods.EventSourcing.Abstractions;

public interface IEventSerializer
{
    /// <summary>
    /// Serializes the given event into a byte array.
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public byte[] Serialize(IEvent @event);

    /// <summary>
    /// Deserializes the given byte array into an event.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public IEvent Deserialize(byte[] data);
}
