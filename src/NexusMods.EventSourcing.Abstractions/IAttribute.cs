using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Interface for a specific attribute
/// </summary>
public interface IAttribute
{
    /// <summary>
    /// The native C# type of the value, must have a matching IValueSerializer registered in the DI container.
    /// </summary>
    public Type ValueType { get; }

    /// <summary>
    /// True if the attribute can have multiple values, false if it can only have a single value.
    /// </summary>
    public bool IsMultiCardinality { get; }

    /// <summary>
    /// True if the attribute's value is a reference to another entity, false if it is a value type.
    /// </summary>
    public bool IsReference { get; }

    /// <summary>
    /// The Unique identifier of the attribute, this is used to match the attribute to a matching attribute
    /// in the datastore
    /// </summary>
    public UInt128 Id { get; }

    /// <summary>
    /// A Human readable name for the attribute, this can be redefined at any time and it has
    /// no impact on the data stored in the datastore
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// A human readable group for the attribute, this can be redefined at any time and it has
    /// no impact on the data stored in the datastore
    /// </summary>
    public string Namespace { get; }
}


/// <summary>
/// Typed variant of IAttribute
/// </summary>
/// <typeparam name="TVal"></typeparam>
public interface IAttribute<TVal> : IAttribute
{

}
