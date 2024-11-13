using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace NexusMods.MnemonicDB.QueryEngine.Abstractions;

/// <summary>
/// An abstract logical variable
/// </summary>
public abstract class LVar : IEquatable<LVar>
{
    private static ulong _nextId = 0;
    
    private readonly ulong _id;
    private readonly string? _name;

    /// <summary>
    /// Primary constructor
    /// </summary>
    protected LVar(ulong id, string? name = null)
    {
        _id = id;
        _name = name;
    }
    
    private static ulong NextId() => Interlocked.Increment(ref _nextId);
    
    /// <summary>
    /// Create a new logical variable of the given type
    /// </summary>
    public static LVar<T> Create<T>(string? name = null) => 
        new(NextId(), name);


    /// <summary>
    /// The value type this variable represents
    /// </summary>
    public abstract Type Type { get; }
    
    /// <summary>
    /// Make a new list of the type of this variable
    /// </summary>
    public abstract IList MakeList<T>();

    /// <inheritdoc />
    public override string ToString() => 
        _name is not null ? $"?{_name}#{_id})" : $"?{_id}";

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not LVar other)
            return false;
        return _id == other._id;
    }

    /// <inheritdoc />
    public bool Equals(LVar? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _id == other._id;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }
}

/// <summary>
/// A logical variable with a specific type
/// </summary>
/// <typeparam name="T">Value type of the logic variable</typeparam>
public sealed class LVar<T>(ulong id, string? name) : LVar(id, name)
{
    /// <inheritdoc />
    public override Type Type => typeof(T);

    /// <inheritdoc />
    public override IList MakeList<T1>() => 
        new List<T1>();
}
