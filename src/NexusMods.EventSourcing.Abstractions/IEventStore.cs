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
    /// <param name="entityId">The Entity Id to playback events for</param>
    /// <param name="ingester">The ingester to handle the events</param>
    /// <param name="reverse">If true, plays the events in reverse</param>
    /// <typeparam name="TIngester"></typeparam>
    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester, bool reverse = false)
        where TIngester : IEventIngester;
}
