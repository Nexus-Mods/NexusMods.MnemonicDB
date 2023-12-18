using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Marker interface for attributes that can be exposed on an entity.
/// </summary>
public interface IAttribute
{
    /// <summary>
    /// The data type of the entity that owns the attribute.
    /// </summary>
    public Type Owner { get; }

    /// <summary>
    /// The name of the attribute, needs to be unique in a given entity but not unique across entities.
    /// </summary>
    public string Name { get; }
}

/// <summary>
/// Marker interface for attributes that expose an accumulator, (which is all of them), but this removes
/// some of the
/// </summary>
/// <typeparam name="TAccumulator"></typeparam>
public interface IAttribute<TAccumulator> : IAttribute where TAccumulator : IAccumulator
{
    /// <summary>
    /// Creates a new empty accumulator for the attribute, this is a factory method to allow the entity context
    /// to lazily create accumulators.
    /// </summary>
    /// <returns></returns>
    public TAccumulator CreateAccumulator();
}

