using System;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// A null value, used to represent a null as a value. This is mostly used for
/// marker attributes that don't have a value.
/// </summary>
public readonly struct Null : IComparable<Null>, IEquatable<Null>, IComparable

{
    private readonly int _dummy; // This is to ensure that the struct is not default

    private Null(int dummy)
    {
        _dummy = dummy;
    }
    
    /// <summary>
    /// A singleton instance of the null value.
    /// </summary>
    public static Null Instance { get; } = new(1);
    
    /// <summary>
    /// True if this is a default value.
    /// </summary>
    public bool IsDefault => _dummy == 0;

    /// <inheritdoc />
    public override string ToString()
    {
        return "Null";
    }
    
    /// <summary>
    /// Compares this instance with another Null instance.
    /// </summary>
    /// <param name="other">The other Null instance to compare with.</param>
    /// <returns>0 if they are equal, -1 if this is default and other is not, 1 if this is not default and other is.</returns>
    public int CompareTo(Null other)
    {
        // Compare based on _dummy value
        return _dummy.CompareTo(other._dummy);
    }
    
    /// <summary>
    /// Implements IComparable interface.
    /// </summary>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is Null other) return CompareTo(other);
        throw new ArgumentException("Object must be of type Null", nameof(obj));
    }
    
    /// <summary>
    /// Determines whether this instance is equal to another Null instance.
    /// </summary>
    public bool Equals(Null other)
    {
        return _dummy == other._dummy;
    }
    
    public static bool operator ==(Null left, Null right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(Null left, Null right)
    {
        return !left.Equals(right);
    }
    
    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Null other && Equals(other);
    }
    
    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _dummy;
    }
}
