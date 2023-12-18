using System.Threading.Tasks;

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
    /// Gets the value of the attribute for the given entity.
    /// </summary>
    /// <param name="ownerId"></param>
    /// <param name="attributeDefinition"></param>
    /// <param name="accumulator"></param>
    /// <typeparam name="TType"></typeparam>
    /// <typeparam name="TOwner"></typeparam>
    /// <typeparam name="TAttribute"></typeparam>
    /// <typeparam name="TAccumulator"></typeparam>
    /// <returns></returns>
    bool GetReadOnlyAccumulator<TOwner, TAttribute, TAccumulator>(EntityId<TOwner> ownerId, TAttribute attributeDefinition, out TAccumulator accumulator)
        where TOwner : IEntity
        where TAttribute : IAttribute<TAccumulator>
        where TAccumulator : IAccumulator;

    /// <summary>
    /// Use only for testing, clears all caches, any existing entities will be stale and likely no longer work
    /// </summary>
    void EmptyCaches();

}
