using System;
using System.Diagnostics;

namespace NexusMods.EventSourcing.Abstractions.AttributeDefinitions;

/// <summary>
/// An attribute for the type of an entity.
/// </summary>
/// <param name="attrName"></param>
public class TypeAttributeDefinition : IAttribute<ScalarAccumulator<Type>>
{
    /// <inheritdoc />
    public ScalarAccumulator<Type> CreateAccumulator()
    {
        return new ScalarAccumulator<Type>();
    }

    /// <inheritdoc />
    public Type Owner => typeof(IEntity);

    /// <inheritdoc />
    public string Name => "$type";

    /// <summary>
    /// Gets the type of the entity for the given entity id.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <typeparam name="TCtx"></typeparam>
    /// <returns></returns>
    public Type Get<TCtx>(TCtx context, EntityId owner) where TCtx : IEntityContext
    {
        if (context.GetReadOnlyAccumulator<IEntity, TypeAttributeDefinition, ScalarAccumulator<Type>>(
                new EntityId<IEntity>(owner), this, out var accumulator))
            return accumulator.Value;
        // TODO, make this a custom exception and extract it to another method
        throw new InvalidOperationException("No type attribute found for entity");
    }

    /// <summary>
    /// Emits information about the type of the entity for the given entity id. The type is emitted as the type of the
    /// entity id.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="id"></param>
    /// <typeparam name="TEventCtx"></typeparam>
    /// <typeparam name="TType"></typeparam>
    public void New<TEventCtx, TType>(TEventCtx context, EntityId<TType> id)
        where TEventCtx : IEventContext
        where TType : IEntity
    {
        if (context.GetAccumulator<IEntity, TypeAttributeDefinition, ScalarAccumulator<Type>>(EntityId<IEntity>.From(id.Value.Value), this, out var accumulator))
            accumulator.Value = typeof(TType);
    }
}
