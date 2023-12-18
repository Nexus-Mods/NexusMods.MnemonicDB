using System;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A collection of entity links to other entities. Think of this as a one to many FK relationship in a
/// database.
/// </summary>
/// <param name="name"></param>
/// <typeparam name="TOwner"></typeparam>
/// <typeparam name="TOther"></typeparam>
public class MultiEntityAttributeDefinition<TOwner, TOther>(string name) : IAttribute<MultiEntityAccumulator<TOther>>
    where TOwner : AEntity<TOwner>, IEntity
    where TOther : IEntity
{
    /// <inheritdoc />
    public MultiEntityAccumulator<TOther> CreateAccumulator()
    {
        return new MultiEntityAccumulator<TOther>();
    }

    /// <summary>
    /// Adds a link to the other entity.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <param name="value"></param>
    /// <typeparam name="TContext"></typeparam>
    public void Add<TContext>(TContext context, EntityId<TOwner> owner, EntityId<TOther> value)
        where TContext : IEventContext
    {
        if (context.GetAccumulator<TOwner, MultiEntityAttributeDefinition<TOwner, TOther>, MultiEntityAccumulator<TOther>>(owner, this, out var accumulator))
            accumulator.Ids.Add(value);
    }

    /// <summary>
    /// Removes a link to the other entity.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <param name="value"></param>
    /// <typeparam name="TContext"></typeparam>
    public void Remove<TContext>(TContext context, EntityId<TOwner> owner, EntityId<TOther> value)
        where TContext : IEventContext
    {
        if (context.GetAccumulator<TOwner, MultiEntityAttributeDefinition<TOwner, TOther>, MultiEntityAccumulator<TOther>>(owner, this, out var accumulator))
            accumulator.Ids.Remove(value);
    }

    /// <summary>
    /// Gets the other entities linked to the given entity.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public IEnumerable<TOther> Get(TOwner entity)
    {
        if (!entity.Context
                .GetReadOnlyAccumulator<TOwner, MultiEntityAttributeDefinition<TOwner, TOther>,
                    MultiEntityAccumulator<TOther>>(entity.Id, this, out var accumulator))
            yield break;
        // This should eventually be cached, and implement INotifyCollectionChanged
        foreach (var id in accumulator.Ids)
            yield return entity.Context.Get(id);

    }

    /// <inheritdoc />
    public Type Owner => typeof(TOwner);

    /// <inheritdoc />
    public string Name => name;
}

public class MultiEntityAccumulator<TType> : IAccumulator
where TType : IEntity
{
    internal readonly HashSet<EntityId<TType>> Ids = new();

}
