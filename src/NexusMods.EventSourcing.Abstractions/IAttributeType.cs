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
public interface IAttributeType<out T>
{
    /// <summary>
    /// Gets the value of the attribute from the current row of the result set
    /// </summary>
    /// <param name="resultSet"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetValue(IResultSet resultSet);
}
