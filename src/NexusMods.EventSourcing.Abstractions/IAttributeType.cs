using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Defines an attribute type, this is the mapping between the DB native types of `ulong` or `string` and the domain types.
/// of `path` or `hash`.
/// </summary>
public interface IAttributeType
{
    /// <summary>
    /// The DB-native type
    /// </summary>
    public ValueTypes ValueType { get; }

    /// <summary>
    /// The unique identifier of the attribute type
    /// </summary>
    public UInt128 UniqueId { get; }

    /// <summary>
    /// Gets the domain type this attribute type maps to
    /// </summary>
    public Type DomainType { get; }
}


/// <summary>
/// A typed attribute type.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IAttributeType<T> : IAttributeType
{
    /// <summary>
    /// Gets the value of the attribute from the current row of the result set
    /// </summary>
    /// <param name="resultSet"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResultSet"></typeparam>
    /// <returns></returns>
    public T GetValue<TResultSet>(TResultSet resultSet) where TResultSet : IResultSet;

    /// <summary>
    /// Write the given value to the sink with the given event, attribute, and time
    /// </summary>
    /// <param name="e"></param>
    /// <param name="a"></param>
    /// <param name="val"></param>
    /// <param name="t"></param>
    /// <param name="sink"></param>
    /// <typeparam name="TSink"></typeparam>
    /// <returns></returns>
    public void Emit<TSink>(ulong e, ulong a, T val, ulong t, IDatomSink sink) where TSink : IDatomSink;
}
