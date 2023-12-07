using System.Threading.Tasks;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// This is the context interface passed to event handlers, it allows the handler to attach new entities to the context
/// </summary>
public interface IEventContext
{

    /// <summary>
    /// Attach an entity to the context, this entity will be tracked by the context and should only be used in events
    /// that intend to create an entity from scratch.
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="entity"></param>
    /// <typeparam name="TEntity"></typeparam>
    public void AttachEntity<TEntity>(EntityId<TEntity> entityId, TEntity entity) where TEntity : IEntity;

    /// <summary>
    /// Retrieve an entity from the context, this may require the context to load the entity via replaying
    /// the events up to the current transaction.
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    public ValueTask<T> Retrieve<T>(EntityId<T> id) where T : IEntity;
}
