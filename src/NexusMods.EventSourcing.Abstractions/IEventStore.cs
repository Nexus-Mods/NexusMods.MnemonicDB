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
    /// <param name="indexed"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TColl"></typeparam>
    /// <returns></returns>
    public TransactionId Add<TEntity, TColl>(TEntity eventEntity, TColl indexed)
        where TEntity : IEvent
        where TColl : IList<(IIndexableAttribute, IAccumulator)>;


    /// <summary>
    /// Gets all events where the given attribute appears in the index with the given value. Transactions
    /// will be between fromTx and toTx (inclusive on both ends)
    /// </summary>
    /// <param name="attr"></param>
    /// <param name="value"></param>
    /// <param name="ingester"></param>
    /// <param name="fromTx"></param>
    /// <param name="toTx"></param>
    /// <typeparam name="TIngester"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    public void EventsForIndex<TIngester, TVal>(IIndexableAttribute<TVal> attr, TVal value, TIngester ingester,
        TransactionId fromTx, TransactionId toTx)
        where TIngester : IEventIngester;

    /// <summary>
    /// For each event for the given entity id, call the ingester.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="ingester">The ingester to handle the events</param>
    /// <param name="attr"></param>
    /// <typeparam name="TIngester"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    public void EventsForIndex<TIngester, TVal>(IIndexableAttribute<TVal> attr, TVal value, TIngester ingester)
        where TIngester : IEventIngester

    {
        EventsForIndex(attr, value, ingester, TransactionId.Min, TransactionId.Max);
    }

    /// <summary>
    /// Gets the most recent snapshot for an entity that was taken before asOf. If no snapshot is found
    /// the TransactionId will be default, otherwise it will be the transaction id of the snapshot. If
    /// the snapshot's entity revision is not equal to the revision parameter, the snapshot is invalid
    /// and default will be returned.
    /// </summary>
    /// <param name="asOf"></param>
    /// <param name="entityId"></param>
    /// <param name="loadedDefinition"></param>
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
