using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.DatomIterators;

/// <summary>
/// Represents a raw (unparsed) datom from an index. Most of the time this datom is only valid for the
/// lifetime of the current iteration. It is not safe to store this datom for later use.
/// </summary>
public readonly struct Datom : IEquatable<Datom>
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
    public KeyPrefix Prefix => _prefix;

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

    /// <inheritdoc />
    public override string ToString()
    {
        return $"[E: {E}, A: {A}, T: {T}, IsRetract: {IsRetract}, Value: {ValueSpan.Length}]";
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
    public int Compare(Datom other, IndexType indexType)
    {
        return indexType switch
        {
            IndexType.TxLog => DatomComparators.TxLogComparator.Compare(this, other),
            IndexType.EAVTCurrent or IndexType.EAVTHistory => DatomComparators.EAVTComparator.Compare(this, other),
            IndexType.AEVTCurrent or IndexType.AEVTHistory => DatomComparators.AEVTComparator.Compare(this, other),
            IndexType.AVETCurrent or IndexType.AVETHistory => DatomComparators.AVETComparator.Compare(this, other),
            IndexType.VAETCurrent or IndexType.VAETHistory => DatomComparators.VAETComparator.Compare(this, other),
            _ => ThrowArgumentOutOfRange(indexType)
        };
    }
    
    private static int ThrowArgumentOutOfRange(IndexType indexType)
    {
        throw new ArgumentOutOfRangeException(nameof(indexType), indexType, "Unknown index type");
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
}
