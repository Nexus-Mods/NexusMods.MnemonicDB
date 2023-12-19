using System.Threading.Tasks;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// An event store is responsible for storing events and retrieving them (by entity id) for replay.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Add an event to the store, returns the transaction id of the insert.
    /// </summary>
    /// <param name="eventEntity"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public TransactionId Add<T>(T eventEntity) where T : IEvent;

    /// <summary>
    /// For each event for the given entity id, call the ingester.
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="ingester"></param>
    /// <typeparam name="TIngester"></typeparam>
    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester)
        where TIngester : IEventIngester;
}
