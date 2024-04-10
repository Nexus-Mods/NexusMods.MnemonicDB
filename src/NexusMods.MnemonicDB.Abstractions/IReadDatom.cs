using System;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Marker interface for a read datom that will contain a TX value.
/// </summary>
public interface IReadDatom
{
    /// <summary>
    /// Entity id of the datom.
    /// </summary>
    public EntityId E { get; }

    /// <summary>
    ///     The value type of the datom, this is used to find the correct serializer.
    /// </summary>
    public Type ValueType { get; }

    public Attribute<ValueType> A { get; }

    /// <summary>
    ///     The transaction id of the datom.
    /// </summary>
    public TxId T { get; }

    /// <summary>
    ///     Gets the value as a object (possibly boxed).
    /// </summary>
    object ObjectValue { get; }

    /// <summary>
    ///     True if this is a retraction of a previous datom.
    /// </summary>
    public bool IsRetract { get; }
}
