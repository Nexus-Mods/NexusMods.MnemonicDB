using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Represents a raw (unparsed) datom from an index. Most of the time this datom is only valid for the
/// lifetime of the current iteration. It is not safe to store this datom for later use.
/// </summary>
public readonly ref struct RefDatom : IEquatable<RefDatom>
{
    private readonly KeyPrefix _prefix;
    private readonly ReadOnlySpan<byte> _valueBlob;

    /// <summary>
    /// Create a new datom from the given prefix and value
    /// </summary>
    public RefDatom(KeyPrefix prefix, ReadOnlySpan<byte> value)
    {
        _prefix = prefix;
        _valueBlob = value;
    }

    /// <summary>
    /// Create a new datom from the given datom memory span and registry
    /// </summary>
    public RefDatom(ReadOnlySpan<byte> datom)
    {
        _prefix = KeyPrefix.Read(datom);
        _valueBlob = datom[KeyPrefix.Size..];
    }

    /// <summary>
    /// Converts the entire datom into a byte array
    /// </summary>
    public byte[] ToArray()
    {
        var array = new byte[KeyPrefix.Size + _valueBlob.Length];
        MemoryMarshal.Write(array, _prefix);
        _valueBlob.CopyTo(array.AsSpan(KeyPrefix.Size));
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
    public ReadOnlySpan<byte> ValueSpan => _valueBlob;
    
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
    public int Compare(RefDatom other) => GlobalComparer.Compare(this, other);


    /// <inheritdoc />
    public bool Equals(RefDatom other)
    {
        return _prefix.Equals(other._prefix) && _valueBlob.SequenceEqual(other._valueBlob);
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
}

