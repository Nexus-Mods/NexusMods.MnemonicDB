using System.Collections.Generic;
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
    /// For each event within the given range (inclusive), for the given entity id, call the ingester.
    /// </summary>
    /// <param name="entityId">The Entity Id to playback events for</param>
    /// <param name="ingester">The ingester to handle the events</param>
    /// <param name="reverse">If true, plays the events in reverse</param>
    /// <typeparam name="TIngester"></typeparam>
    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester, TransactionId fromId, TransactionId toId)
        where TIngester : IEventIngester;

    /// <summary>
    /// For each event for the given entity id, call the ingester.
    /// </summary>
    /// <param name="entityId">The Entity Id to playback events for</param>
    /// <param name="ingester">The ingester to handle the events</param>
    /// <param name="reverse">If true, plays the events in reverse</param>
    /// <typeparam name="TIngester"></typeparam>
    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester)
        where TIngester : IEventIngester

    {
        EventsForEntity(entityId, ingester, TransactionId.Min, TransactionId.Max);
    }

    /// <summary>
    /// Gets the most recent snapshot for an entity that was taken before asOf. If no snapshot is found
    /// the TransactionId will be default, otherwise it will be the transaction id of the snapshot. If
    /// the snapshot's entity revision is not equal to the revision parameter, the snapshot is invalid
    /// and default will be returned.
    /// </summary>
    /// <param name="asOf"></param>
    /// <param name="entityId"></param>
    /// <param name="revision"></param>
    /// <param name="loadedAttributes"></param>
    /// <returns></returns>
    public TransactionId GetSnapshot(TransactionId asOf, EntityId entityId,
        out IAccumulator loadedDefinition,
        out (IAttribute Attribute, IAccumulator Accumulator)[] loadedAttributes);

    /// <summary>
    /// Sets the snapshot for the given entity id and transaction id.
    /// </summary>
    /// <param name="txId"></param>
    /// <param name="id"></param>
    /// <param name="attributes"></param>
    public void SetSnapshot(TransactionId txId, EntityId id, IDictionary<IAttribute, IAccumulator> attributes);
}
