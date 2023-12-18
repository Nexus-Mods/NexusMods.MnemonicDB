using System;
using System.Diagnostics;

namespace NexusMods.EventSourcing.Abstractions.AttributeDefinitions;

/// <summary>
/// An attribute for the type of an entity.
/// </summary>
/// <param name="attrName"></param>
public class TypeAttributeDefinition : IAttribute<TypeAccumulator>
{
    /// <inheritdoc />
    public TypeAccumulator CreateAccumulator()
    {
        return new TypeAccumulator();
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
        return context.GetAccumulator<IEntity, TypeAttributeDefinition, TypeAccumulator>(owner, this)
            .Get();
    }
}


/// <summary>
/// An accumulator for the type of an entity.
/// </summary>
public class TypeAccumulator : IAccumulator
{
    private Type _type = null!;

    /// <summary>
    /// Sets the value of the accumulator, can only ever be set once for a given accumulator.
    /// </summary>
    /// <param name="type"></param>
    public void Set(Type type)
    {
        Debug.Assert(_type == null, "Type attribute can only ever be set once");
        _type = type;
    }

    /// <summary>
    /// Retrieves the value of the accumulator.
    /// </summary>
    /// <returns></returns>
    public Type Get()
    {
        Debug.Assert(_type != null, "Type attribute must be set before it can be retrieved");
        return _type;
    }
}
