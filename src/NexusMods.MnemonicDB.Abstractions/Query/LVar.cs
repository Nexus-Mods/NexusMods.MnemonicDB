using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NexusMods.MnemonicDB.Abstractions.Query;

/// <summary>
/// A logic variable, that can be used to represent values in a query.
/// </summary>
public abstract class LVar : IEquatable<LVar>
{
    /// <summary>
    /// 
    /// </summary>
    private static ulong _nextId = 0;
    
    /// <summary>
    /// Get the type of value this variable can represent.
    /// </summary>
    public abstract Type Type { get; }
    
    /// <summary>
    /// Create a new logic variable of the given value type
    /// </summary>
    public static LVar<T> Create<T>(string? name = null)
    {
        return new LVar<T>(Interlocked.Increment(ref _nextId), name);
    }
    
    /// <summary>
    /// Get the next available id for a logic variable
    /// </summary>
    public static ulong NextId()
    {
        return Interlocked.Increment(ref _nextId);
    }

    /// <inheritdoc />
    public bool Equals(LVar? other) => ReferenceEquals(this, other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj);

    /// <inheritdoc />
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}

/// <summary>
/// A logic variable, that can be used to represent values in a query of a given type
/// </summary>
/// <typeparam name="T"></typeparam>
public class LVar<T> : LVar, IEquatable<LVar<T>>
{
    private readonly string _name;
    internal LVar(ulong id, string? name = null)
    {
        _name = name == null ? $"?{id}" : $"?{name}:{id}";
    }
    
    /// <inheritdoc />
    public override Type Type => typeof(T);


    /// <inheritdoc />
    public override string ToString() => _name;

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj);

    /// <inheritdoc />
    public bool Equals(LVar<T>? other)
    {
        return ReferenceEquals(this, other);
    }

    /// <inheritdoc />
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    
    /// <summary>
    /// Implicit conversion from a tracked value to a logic variable (for use in queries)
    /// </summary>
    public static implicit operator LVar<T>(TrackingDynamicObject.TrackedValue tv) => tv.AsLVar<T>();
}

