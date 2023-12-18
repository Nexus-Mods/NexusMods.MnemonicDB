using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A scalar attribute that can be exposed on an entity.
/// </summary>
public abstract class AScalarAttribute<TOwner, TType>(string attrName) : IAttribute<Accumulator<TType>>
where TOwner : IEntity
{
    /// <inheritdoc />
    public bool IsScalar => false;

    /// <inheritdoc />
    public Type Owner => typeof(TOwner);

    /// <inheritdoc />
    public string Name => attrName;

    /// <inheritdoc />
    public Accumulator<TType> CreateAccumulator()
    {
        return new Accumulator<TType>();
    }

    /// <summary>
    /// Sets the value of the attribute for the given entity.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <param name="value"></param>
    /// <typeparam name="TContext"></typeparam>
    public void Set<TContext>(TContext context, EntityId<TOwner> owner, TType value)
        where TContext : IEventContext
    {
        var accumulator = context.GetAccumulator<TOwner, AScalarAttribute<TOwner, TType>, Accumulator<TType>>(owner, this);
        accumulator.Set(value!);
    }

    /// <summary>
    /// Resets the value of the attribute for the given entity to the default value.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <typeparam name="TContext"></typeparam>
    public void Unset<TContext>(TContext context, EntityId<TOwner> owner)
        where TContext : IEventContext
    {
        var accumulator = context.GetAccumulator<TOwner, AScalarAttribute<TOwner, TType>, Accumulator<TType>>(owner, this);
        accumulator.Unset();
    }

    /// <summary>
    /// Gets the value of the attribute for the given entity.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="owner"></param>
    /// <typeparam name="TContext"></typeparam>
    /// <returns></returns>
    public TType Get<TContext>(TContext context, EntityId<TOwner> owner)
        where TContext : IEventContext
    {
        var accumulator = context.GetAccumulator<TOwner, AScalarAttribute<TOwner, TType>, Accumulator<TType>>(owner, this);
        return accumulator.Get();
    }


    /// <summary>
    /// The data type of the attribute.
    /// </summary>
    public Type AttributeType => typeof(TType);
}

/// <summary>
/// A scalar attribute accumulator, used to store a single value
/// </summary>
/// <typeparam name="TVal"></typeparam>
public class Accumulator<TVal> : IAccumulator
{
    private TVal _value = default! ;

    /// <summary>
    /// Sets the value of the accumulator
    /// </summary>
    /// <param name="value"></param>
    public void Set(TVal value)
    {
        _value = value;
    }

    /// <summary>
    /// Resets the value of the accumulator to a default value
    /// </summary>
    /// <param name="value"></param>
    public void Unset()
    {
        _value = default!;
    }

    /// <summary>
    /// Gets the value of the accumulator
    /// </summary>
    /// <returns></returns>
    public TVal Get()
    {
        return _value!;
    }
}
