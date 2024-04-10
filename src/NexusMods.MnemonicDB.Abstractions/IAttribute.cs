using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

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
    ///     True if the attribute's value is a reference to another entity, false if it is a value type.
    /// </summary>
    public bool IsReference { get; }

    /// <summary>
    ///     The Unique identifier of the attribute, this is used to match the attribute to a matching attribute
    ///     in the datastore
    /// </summary>
    public Symbol Id { get; }

    /// <summary>
    /// Gets the unique id of the attribute for the given registry
    /// </summary>
    public AttributeId GetDbId(RegistryId id);

    /// <summary>
    /// Sets the unique id of the attribute for the given registry
    /// </summary>
    /// <param name="id"></param>
    /// <param name="attributeId"></param>
    public void SetDbId(RegistryId id, AttributeId attributeId);

    /// <summary>
    /// Sets the serializer for the attribute
    /// </summary>
    /// <param name="serializer"></param>
    public void SetSerializer(IValueSerializer serializer);

    /// <summary>
    ///    True if the attribute is indexed, false if it is not.
    /// </summary>
    bool IsIndexed { get; }

    /// <summary>
    ///   True if the attribute has no history, false if it does.
    /// </summary>
    bool NoHistory { get; }

    /// <summary>
    ///   The cardinality of the attribute
    /// </summary>
    Cardinality Cardinalty { get; }

    /// <summary>
    ///   The serializer for the attribute
    /// </summary>
    IValueSerializer Serializer { get; }

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
    ///     Construct a new write Datom for the given entity and value
    /// </summary>
    public IWriteDatom Assert(EntityId e, TVal v);

    /// <summary>
    ///     Construct a new write Datom for the retraction of the given entity and value
    /// </summary>
    public IWriteDatom Retract(EntityId e, TVal v);

    /// <summary>
    /// Gets the serializer for the attribute
    /// </summary>
    public IValueSerializer<TVal> Serializer { get; }
}
