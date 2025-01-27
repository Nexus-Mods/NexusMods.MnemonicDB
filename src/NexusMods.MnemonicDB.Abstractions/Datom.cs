using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.DatomIterators;

/// <summary>
/// Represents a raw (unparsed) datom from an index. Most of the time this datom is only valid for the
/// lifetime of the current iteration. It is not safe to store this datom for later use.
/// </summary>
public readonly struct Datom : IEquatable<Datom>, IComparable<Datom>
{
    private readonly KeyPrefix _prefix;
    private readonly ReadOnlyMemory<byte> _valueBlob;

    /// <summary>
    /// Create a new datom from the given prefix and value
    /// </summary>
    public Datom(in KeyPrefix prefix, ReadOnlyMemory<byte> value)
    {
        _prefix = prefix;
        _valueBlob = value;
    }

    /// <summary>
    /// Create a new datom from the given datom memory span and registry
    /// </summary>
    public Datom(ReadOnlyMemory<byte> datom)
    {
        _prefix = KeyPrefix.Read(datom.Span);
        _valueBlob = datom[KeyPrefix.Size..];
    }

    /// <summary>
    /// Converts the entire datom into a byte array
    /// </summary>
    public byte[] ToArray()
    {
        var array = new byte[KeyPrefix.Size + _valueBlob.Length];
        MemoryMarshal.Write(array, _prefix);
        _valueBlob.Span.CopyTo(array.AsSpan(KeyPrefix.Size));
        return array;
    }
    

    /// <summary>
    /// The KeyPrefix of the datom
    /// </summary>
    public KeyPrefix Prefix
    {
        get => _prefix;
        init => _prefix = value;
    }

    /// <summary>
    /// The valuespan of the datom
    /// </summary>
    public ReadOnlySpan<byte> ValueSpan => _valueBlob.Span;
    
    /// <summary>
    /// The value memory of the datom
    /// </summary>
    public ReadOnlyMemory<byte> ValueMemory => _valueBlob;
    
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
    /// Copies the data of this datom onto the heap so it's detached from the current iteration.
    /// </summary>
    public Datom Clone()
    {
        return new Datom(_prefix, _valueBlob.ToArray());
    }

    /// <summary>
    /// Returns true if the datom is valid
    /// </summary>
    public bool Valid => _prefix.IsValid;

    /// <summary>
    /// The minimum most possible datom
    /// </summary>
    public static Datom Min => new(KeyPrefix.Min, ReadOnlyMemory<byte>.Empty);
    
    /// <summary>
    /// The maximum most possible datom
    /// </summary>
    public static Datom Max => new(KeyPrefix.Max, ReadOnlyMemory<byte>.Empty);

    /// <inheritdoc />
    public override string ToString()
    {
        if (_prefix.Index == IndexType.None) 
            return $"[E: {E}, A: {A}, T: {T}, IsRetract: {IsRetract}, Value: {Prefix.ValueTag.Read<object>(ValueSpan)}]";
        return $"[Index: {_prefix.Index}, E: {E}, A: {A}, T: {T}, IsRetract: {IsRetract}, Value: {Prefix.ValueTag.Read<object>(ValueSpan)}]";
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
    public int Compare(Datom other) => GlobalComparer.Compare(in this, in other);
    
    /// <summary>
    /// Return a copy of this datom with the given index set as the index
    /// </summary>
    public Datom WithIndex(IndexType index)
    {
        return new Datom(_prefix with {Index = index}, _valueBlob);
    }

    /// <summary>
    /// Clone this datom and return it as a retraction datom
    /// </summary>
    /// <returns></returns>
    public Datom Retract()
    {
        return new Datom(_prefix with {IsRetract = true, T = TxId.Tmp}, _valueBlob);
    }

    /// <inheritdoc />
    public bool Equals(Datom other)
    {
        return _prefix.Equals(other._prefix) && _valueBlob.Span.SequenceEqual(other._valueBlob.Span);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Datom other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(_prefix.GetHashCode());
    }

    /// <inheritdoc />
    public int CompareTo(Datom other)
    {
        return Compare(other);
    }
}
