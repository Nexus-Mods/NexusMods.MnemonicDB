using System;
using System.Threading.Tasks;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// This is the context interface passed to event handlers, it allows the handler to attach new entities to the context
/// </summary>
public interface IEventContext
{

    /// <summary>
    /// Emits a new value for the given attribute on the given entity
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="attr"></param>
    /// <param name="value"></param>
    /// <typeparam name="TOwner"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    public void Emit<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value)
        where TOwner : IEntity;

    /// <summary>
    /// Emits a new member value for the given attribute on the given entity
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="attr"></param>
    /// <param name="value"></param>
    /// <typeparam name="TOwner"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    public void Emit<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr,
        EntityId<TVal> value)
        where TOwner : IEntity
        where TVal : IEntity;


    /// <summary>
    /// Retracts a value for the given attribute on the given entity
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="attr"></param>
    /// <param name="value"></param>
    /// <typeparam name="TOwner"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    public void Retract<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value)
        where TOwner : IEntity;

    /// <summary>
    /// Retracts a member value for the given attribute on the given entity
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="attr"></param>
    /// <param name="value"></param>
    /// <typeparam name="TOwner"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    public void Retract<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr,
        EntityId<TVal> value)
        where TOwner : IEntity
        where TVal : IEntity;

    /// <summary>
    /// Emits the type attribute for the given entity so that polymorphic queries can be performed
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="TType"></typeparam>
    /// <exception cref="NotImplementedException"></exception>
    public void New<TType>(EntityId<TType> id) where TType : IEntity;
}
