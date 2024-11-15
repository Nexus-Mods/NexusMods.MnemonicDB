using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.Iterators;

/// <summary>
/// A datom that can only exist on the stack, a readonly struct used mostly for iteration
/// </summary>
public readonly ref struct RefDatom : IEquatable<RefDatom>, IComparable<RefDatom>
{
    /// <summary>
    /// The key prefix portion of the datom (all the components of the datom except the value)
    /// </summary>
    public readonly KeyPrefix Prefix;
    
    /// <summary>
    /// The value portion of the datom represented as a span
    /// </summary>
    public readonly ReadOnlySpan<byte> ValueSpan;

    /// <summary>
    /// Create a new datom from the given prefix and value
    /// </summary>
    public RefDatom(KeyPrefix prefix, ReadOnlySpan<byte> value)
    {
        Prefix = prefix;
        ValueSpan = value;
    }

    /// <summary>
    /// Create a new datom from the given datom memory span and registry
    /// </summary>
    public RefDatom(ReadOnlySpan<byte> datom)
    {
        Prefix = KeyPrefix.Read(datom);
        ValueSpan = datom[KeyPrefix.Size..];
    }
    
    /// <summary>
    /// EntityId of the datom
    /// </summary>
    public EntityId E => Prefix.E;

    /// <summary>
    /// AttributeId of the datom
    /// </summary>
    public AttributeId A => Prefix.A;

    /// <summary>
    /// TxId of the datom
    /// </summary>
    public TxId T => Prefix.T;

    /// <summary>
    /// True if the datom is a retract
    /// </summary>
    public bool IsRetract => Prefix.IsRetract;

    /// <summary>
    /// Returns true if the datom is valid
    /// </summary>
    public bool Valid => Prefix.IsValid;

    /// <inheritdoc />
    public override string ToString()
    {
        if (Prefix.Index == IndexType.None) 
            return $"[E: {E}, A: {A}, T: {T}, IsRetract: {IsRetract}, Value: {Prefix.ValueTag.Read<object>(ValueSpan)}]";
        return $"[Index: {Prefix.Index}, E: {E}, A: {A}, T: {T}, IsRetract: {IsRetract}, Value: {Prefix.ValueTag.Read<object>(ValueSpan)}]";
    }

    /// <summary>
    /// Returns the resolved version of this datom
    /// </summary>
    public IReadDatom Resolved(AttributeResolver resolver)
    {
        return resolver.Resolve(this);
    }
    
    /// <summary>
    /// Returns the resolved version of this datom
    /// </summary>
    public IReadDatom Resolved(IConnection conn)
    {
        return conn.AttributeResolver.Resolve(this);
    }

    /// <summary>
    /// Returns -1 if this datom is less than the other, 0 if they are equal, and 1 if this datom is greater than the other.
    /// in relation to the given index type.
    /// </summary>
    public int CompareTo(RefDatom other) => GlobalComparer.Compare(this, in other);
    
    /// <summary>
    /// Return a copy of this datom with the given index set as the index
    /// </summary>
    public RefDatom WithIndex(IndexType index)
    {
        var prefix = Prefix with { Index = index };
        return new RefDatom(prefix, ValueSpan);
    }

    /// <summary>
    /// Clone this datom and return it as a retraction datom
    /// </summary>
    /// <returns></returns>
    public RefDatom Retract()
    {
        return new RefDatom(Prefix with {IsRetract = true, T = TxId.Tmp}, ValueSpan);
    }

    /// <inheritdoc />
    public bool Equals(RefDatom other)
    {
        return Prefix.Equals(other.Prefix) && ValueSpan.SequenceEqual(other.ValueSpan);
    }


    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Datom other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Prefix.GetHashCode());
    }
}
