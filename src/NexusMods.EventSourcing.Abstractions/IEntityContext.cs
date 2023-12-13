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
    /// Transacts a new event into the context.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    public ValueTask Add<TEvent>(TEvent entity) where TEvent : IEvent;


    /// <summary>
    /// Gets the value of the attribute for the given entity.
    /// </summary>
    /// <param name="ownerId"></param>
    /// <param name="attributeDefinition"></param>
    /// <typeparam name="TType"></typeparam>
    /// <typeparam name="TOwner"></typeparam>
    /// <returns></returns>
    IAccumulator GetAccumulator<TType, TOwner>(EntityId ownerId, AttributeDefinition<TOwner,TType> attributeDefinition) where TOwner : IEntity;

}
