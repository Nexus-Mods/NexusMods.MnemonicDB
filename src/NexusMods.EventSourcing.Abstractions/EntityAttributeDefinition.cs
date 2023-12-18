using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A attribute that contains a lazy reference to another entity. The link can be updated via setting the entity Id, and
/// retrieved via the <see cref="Get{TContext}"/> method. Think of this attribute like a foreign key in a database.
/// </summary>
/// <param name="attrName"></param>
/// <typeparam name="TOwner"></typeparam>
/// <typeparam name="TOther"></typeparam>
public class EntityAttributeDefinition<TOwner, TOther>(string attrName) : IAttribute<ScalarAccumulator<EntityId<TOther>>>
    where TOwner : AEntity<TOwner>
    where TOther : IEntity
{
    /// <inheritdoc />
    public ScalarAccumulator<EntityId<TOther>> CreateAccumulator()
    {
        return new ScalarAccumulator<EntityId<TOther>>();
    }

    /// <summary>
    /// Set the link to the other entity.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <param name="value"></param>
    /// <typeparam name="TContext"></typeparam>
    public void Link<TContext>(TContext context, EntityId<TOwner> owner, EntityId<TOther> value)
        where TContext : IEventContext
    {
        if (context.GetAccumulator<TOwner, EntityAttributeDefinition<TOwner, TOther>, ScalarAccumulator<EntityId<TOther>>>(owner, this, out var accumulator))
            accumulator.Value = value;
    }

    /// <summary>
    /// Removes the link to the other entity.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <typeparam name="TContext"></typeparam>
    public void Unlink<TContext>(TContext context, EntityId<TOwner> owner)
        where TContext : IEventContext
    {
        if (context.GetAccumulator<TOwner, EntityAttributeDefinition<TOwner, TOther>, ScalarAccumulator<EntityId<TOther>>>(owner, this, out var accumulator))
            accumulator.Value = default!;
    }

    /// <summary>
    /// Gets the value of the attribute for the given entity.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <typeparam name="TContext"></typeparam>
    /// <returns></returns>
    public TOther Get(TOwner owner)
    {
        if (owner.Context.GetReadOnlyAccumulator<TOwner, ScalarAttribute<TOwner, TType>, ScalarAccumulator<TType>>(owner.Id, this, out var accumulator))
            return owner.Context.Get<TOther>(accumulator.Value);
        // TODO, make this a custom exception and extract it to another method
        throw new InvalidOperationException($"Attribute not found for {Name} on {Owner.Name} with id {owner.Id}");
    }

    public Type Owner => typeof(TOwner);
    public string Name => attrName;
}
