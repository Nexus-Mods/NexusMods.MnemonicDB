using System;
using System.Runtime.CompilerServices;
using NexusMods.MneumonicDB.Abstractions.Models;

namespace NexusMods.MneumonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
public interface IAttribute
{
    /// <summary>
    ///     The native C# type of the value, must have a matching IValueSerializer registered in the DI container.
    /// </summary>
    public Type ValueType { get; }

    /// <summary>
    ///     True if the attribute can have multiple values, false if it can only have a single value.
    /// </summary>
    public bool IsMultiCardinality { get; }

    /// <summary>
    ///     True if the attribute's value is a reference to another entity, false if it is a value type.
    /// </summary>
    public bool IsReference { get; }

    /// <summary>
    ///     The Unique identifier of the attribute, this is used to match the attribute to a matching attribute
    ///     in the datastore
    /// </summary>
    public Symbol Id { get; }

    bool IsIndexed { get; }
    bool NoHistory { get; }

    /// <summary>
    ///     Sets the serializer for the attribute, this is used to read and write the value from the buffer
    /// </summary>
    /// <param name="serializer"></param>
    public void SetSerializer(IValueSerializer serializer);

    /// <summary>
    ///     Converts the given values into a typed datom
    /// </summary>
    IReadDatom Resolve(EntityId entityId, AttributeId attributeId, ReadOnlySpan<byte> value, TxId tx, bool isRetract);

    /// <summary>
    ///     Gets the type of the read datom for the given attribute.
    /// </summary>
    Type GetReadDatomType();

}

/// <summary>
///     Typed variant of IAttribute
/// </summary>
/// <typeparam name="TVal"></typeparam>
public interface IAttribute<TVal> : IAttribute
{
    /// <summary>
    ///     Creates a new assertion datom for the given entity and value
    /// </summary>
    public static abstract void Add(ITransaction tx, EntityId entity, TVal value);

    /// <summary>
    ///     Construct a new write Datom for the given entity and value
    /// </summary>
    /// <param name="e"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static abstract IWriteDatom Assert(EntityId e, TVal v);

    /// <summary>
    ///     Construct a new write Datom for the retraction of the given entity and value
    /// </summary>
    /// <param name="e"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static abstract IWriteDatom Retract(EntityId e, TVal v);

    /// <summary>
    /// Gets the serializer for the attribute
    /// </summary>
    public IValueSerializer<TVal> Serializer { get; }
}
