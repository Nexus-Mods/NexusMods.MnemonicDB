using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public struct EntityContextIngester(Dictionary<IAttribute, IAccumulator> values, EntityId id) : IEventContext, IEventIngester
{
    public void Ingest(IEvent @event)
    {
        @event.Apply(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IAccumulator GetAccumulator<TAttribute>(TAttribute attributeDefinition)
        where TAttribute : IAttribute
    {
        if (values.TryGetValue(attributeDefinition, out var accumulator))
        {
            return accumulator;
        }

        accumulator = attributeDefinition.CreateAccumulator();
        values.Add(attributeDefinition, accumulator);
        return accumulator;
    }

    public void Emit<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value) where TOwner : IEntity
    {
        if (entity.Value != id) return;

        var accumulator = GetAccumulator(attr);
        accumulator.Add(value!);
    }

    public void Emit<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
    {
        if (entity.Value != id) return;

        var accumulator = GetAccumulator(attr);
        accumulator.Add(value!);
    }

    public void Retract<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value) where TOwner : IEntity
    {
        if (entity.Value != id) return;

        var accumulator = GetAccumulator(attr);
        accumulator.Retract(value!);
    }

    public void Retract<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
    {
        if (entity.Value != id) return;

        var accumulator = GetAccumulator(attr);
        accumulator.Retract(value!);
    }

    public void New<TType>(EntityId<TType> newId) where TType : IEntity
    {
        if (newId.Value != id) return;

        var accumulator = GetAccumulator(IEntity.TypeAttribute);
        accumulator.Add(typeof(TType));
    }

}
