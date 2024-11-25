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

    /// <summary>
    /// The attribute of the datom.
    /// </summary>
    public IAttribute A { get; }

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

    /// <summary>
    /// Adds the datom to the transaction as a retraction.
    /// </summary>
    void Retract(ITransaction tx);

    /// <summary>
    /// Returns true if the datom is equal when comparing the entity, attribute and value.
    /// </summary>
    bool EqualsByValue(IReadDatom other);


    /// <summary>
    /// Hashcode of just the entity, attribute and value.
    /// </summary>
    public int HashCodeByValue();
}

/// <summary>
/// A read datom that contains a value of type V.
/// </summary>
public interface IReadDatom<out TV> : IReadDatom
{
    /// <summary>
    /// Get the value of the datom.
    /// </summary>
    public TV Value { get; }
}

/// <summary>
/// A wrapper around a datom that compares only on the EAV values
/// </summary>
public readonly struct ReadDatomKey : IEquatable<ReadDatomKey>
{
    private readonly IReadDatom _datom;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadDatomKey"/> struct.
    /// </summary>
    public ReadDatomKey(IReadDatom datom)
    {
        _datom = datom;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not ReadDatomKey key)
            return false;

        return _datom.EqualsByValue(key._datom);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _datom.HashCodeByValue();
    }

    /// <inheritdoc />
    public bool Equals(ReadDatomKey other)
    {
        return _datom.EqualsByValue(other._datom);
    }

}
