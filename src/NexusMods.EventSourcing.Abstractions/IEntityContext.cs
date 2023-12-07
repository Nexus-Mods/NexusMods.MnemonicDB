using System.Threading.Tasks;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A context for working with entities and events. Multiple contexts can be used to work with different sets of entities
/// as of different transaction ids.
/// </summary>
public interface IEntityContext
{
    /// <summary>
    /// Adds the event to the event store, and advances the "as of" transaction id to the transaction id of the event.
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public ValueTask Transact(IEvent @event);

    /// <summary>
    /// Get the entity with the given id from the context, the entity will be up-to-date as of the current "as of" transaction id.
    /// </summary>
    /// <param name="entityId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ValueTask<T> Retrieve<T>(EntityId<T> entityId) where T : IEntity;

    /// <summary>
    /// The current "as of" transaction id. The entities in this context are up-to-date as of this transaction id.
    /// </summary>
    public TransactionId AsOf { get; }

    /// <summary>
    /// Advances the "as of" transaction id to the given transaction id, all objects in this context will be updated
    /// to reflect the new transaction id.
    /// </summary>
    /// <param name="transactionId"></param>
    /// <returns></returns>
    public ValueTask Advance(TransactionId transactionId);

    /// <summary>
    /// Advances the "as of" transaction id to the most recent transaction id, all objects in this context will be updated
    /// to reflect the new transaction id.
    /// </summary>
    /// <returns></returns>
    public ValueTask Advance();


}
