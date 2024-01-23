using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Abstractions.AttributeDefinitions;

/// <summary>
/// An attribute for the type of an entity.
/// </summary>
public class TypeAttributeDefinition : IAttribute<ScalarAccumulator<EntityDefinition>>
{
    /// <inheritdoc />
    public ScalarAccumulator<EntityDefinition> CreateAccumulator()
    {
        return new ScalarAccumulator<EntityDefinition>();
    }

    /// <inheritdoc />
    public Type Owner => typeof(IEntity);

    /// <inheritdoc />
    public string Name => "$type";

    IAccumulator IAttribute.CreateAccumulator()
    {
        return CreateAccumulator();
    }

    /// <summary>
    /// Gets the type of the entity for the given entity id.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <typeparam name="TCtx"></typeparam>
    /// <returns></returns>
    public Type Get<TCtx>(TCtx context, EntityId owner) where TCtx : IEntityContext
    {
        if (context.GetReadOnlyAccumulator<IEntity, TypeAttributeDefinition, ScalarAccumulator<EntityDefinition>>(owner, this, out var accumulator))
            return accumulator.Value.Type;
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
        var definition = EntityStructureRegistry.GetDefinition<TType>();
        if (context.GetAccumulator<IEntity, TypeAttributeDefinition, ScalarAccumulator<EntityDefinition>>(EntityId<IEntity>.From(id.Id), IEntity.TypeAttribute, out var accumulator))
            accumulator.Value = definition;
    }
}
