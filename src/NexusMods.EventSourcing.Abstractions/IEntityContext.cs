namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A context for working with entities and events. Multiple contexts can be used to work with different sets of entities
/// as of different transaction ids.
/// </summary>
public interface IEntityContext
{
    /// <summary>
    /// Gets the entity with the specified id.
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public TEntity Get<TEntity>(EntityId<TEntity> id) where TEntity : IEntity;


    /// <summary>
    /// Gets the singleton entity of the specified type.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public TEntity Get<TEntity>() where TEntity : ISingletonEntity;


    /// <summary>
    /// Transacts a new event into the context.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    public TransactionId Add<TEvent>(TEvent entity) where TEvent : IEvent;

    /// <summary>
    /// Starts a new transaction, events can be added to the transaction, then applied
    /// at once by calling commit on the transaction.
    /// </summary>
    /// <returns></returns>
    public ITransaction Begin();

    /// <summary>
    /// Gets the value of the attribute for the given entity. If createIfMissing is true, then the attribute will be
    /// created if it does not exist, this is useful for attributes that have a default "empty" value.
    /// </summary>
    /// <param name="ownerId"></param>
    /// <param name="attributeDefinition"></param>
    /// <param name="accumulator"></param>
    /// <param name="createIfMissing"></param>
    /// <typeparam name="TOwner"></typeparam>
    /// <typeparam name="TAttribute"></typeparam>
    /// <typeparam name="TAccumulator"></typeparam>
    /// <returns></returns>
    bool GetReadOnlyAccumulator<TOwner, TAttribute, TAccumulator>(EntityId<TOwner> ownerId, TAttribute attributeDefinition, out TAccumulator accumulator, bool createIfMissing = false)
        where TOwner : IEntity
        where TAttribute : IAttribute<TAccumulator>
        where TAccumulator : IAccumulator;

    /// <summary>
    /// Use only for testing, clears all caches, any existing entities will be stale and likely no longer work
    /// </summary>
    void EmptyCaches();

}
