using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Interface for a specific attribute
/// </summary>
public interface IAttribute
{
    /// <summary>
    /// Sets the serializer for the attribute, this is used to read and write the value from the buffer
    /// </summary>
    /// <param name="serializer"></param>
    public void SetSerializer(IValueSerializer serializer);

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
    public Symbol Id { get; }
}


/// <summary>
/// Typed variant of IAttribute
/// </summary>
/// <typeparam name="TVal"></typeparam>
public interface IAttribute<TVal> : IAttribute
{
    /// <summary>
    /// Creates a new assertion datom for the given entity and value
    /// </summary>
    public static abstract void Add(ITransaction tx, EntityId entity, TVal value);
}
