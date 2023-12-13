using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Marker interface for attributes that can be exposed on an entity.
/// </summary>
public interface IAttribute
{
    /// <summary>
    /// True if the attribute is a scalar, false if it is a collection.
    /// </summary>
    public bool IsScalar { get; }

    /// <summary>
    /// The data type of the entity that owns the attribute.
    /// </summary>
    public Type Owner { get; }

    /// <summary>
    /// The name of the attribute, needs to be unique in a given entity but not unique across entities.
    /// </summary>
    public string Name { get; }


    /// <summary>
    /// Creates a new accumulator for the attribute.
    /// </summary>
    /// <returns></returns>
    public IAccumulator CreateAccumulator();

}

