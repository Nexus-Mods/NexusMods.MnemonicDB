using System.Collections.Concurrent;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public readonly struct ForwardEventContext(ConcurrentDictionary<EntityId, Dictionary<IAttribute, IAccumulator>> trackedValues) : IEventContext
{

    private IAccumulator? GetAccumulator<TAttribute>(EntityId id, TAttribute attributeDefinition)
        where TAttribute : IAttribute
    {
        if (!trackedValues.TryGetValue(id, out var values)) return null;

        if (values.TryGetValue(attributeDefinition, out var accumulator))
        {
            return accumulator;
        }

        var newAccumulator = attributeDefinition.CreateAccumulator();
        values.Add(attributeDefinition, newAccumulator);
        return newAccumulator;
    }

    public void Emit<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value) where TOwner : IEntity
    {
        var accumulator = GetAccumulator(entity.Value, attr);
        accumulator?.Add(value!);
    }

    public void Emit<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
    {
        var accumulator = GetAccumulator(entity.Value, attr);
        accumulator?.Add(value!);
    }

    public void Retract<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value) where TOwner : IEntity
    {
        var accumulator = GetAccumulator(entity.Value, attr);
        accumulator?.Retract(value!);
    }

    public void Retract<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
    {
        var accumulator = GetAccumulator(entity.Value, attr);
        accumulator?.Retract(value!);
    }

    public void New<TType>(EntityId<TType> id) where TType : IEntity
    {
        // Do nothing, as this entity should be pulled fresh from the store when needed
    }
}
