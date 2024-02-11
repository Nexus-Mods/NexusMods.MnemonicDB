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

    /// <summary>
    /// Reads the value from the buffer and returns all the data as a Datom
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="tx"></param>
    /// <param name="isAssert"></param>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public IDatom Read(ulong entity, ulong tx, bool isAssert, ReadOnlySpan<byte> buffer);
}


/// <summary>
/// Typed variant of IAttribute
/// </summary>
/// <typeparam name="TVal"></typeparam>
public interface IAttribute<TVal> : IAttribute
{

}
