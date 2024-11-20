using System;

namespace NexusMods.MnemonicDB.QueryEngine;

/// <summary>
/// An interface for terms in a query
/// </summary>
public interface ITerm
{
    /// <summary>
    /// Return the type of the term
    /// </summary>
    public Type Type { get; }
    
    /// <summary>
    /// Returns true if the term is an LVar
    /// </summary>
    public bool IsLVar { get; }
    
    /// <summary>
    /// Returns true if the term is a value
    /// </summary>
    public bool IsValue { get; }
    
    /// <summary>
    /// Get the abstract LVar of the term if it is an LVar
    /// </summary>
    public LVar LVar { get; }
}

/// <summary>
/// A term is a slot for either a lvar or a value of a given type. Often used in
/// predicates that can have parameters that are either constants or lvars
/// </summary>
public struct Term<T> : ITerm
{
    private LVar<T>? _lvar;
    private T _value;
    
    /// <summary>
    /// Initializes a new term with a LVar
    /// </summary>
    public Term(LVar<T> lvar)
    {
        _lvar = lvar;
        _value = default!;
    }
    
    /// <summary>
    /// Initializes a new term with a value
    /// </summary>
    public Term(T value)
    {
        _lvar = null;
        _value = value;
    }

    /// <inheritdoc />
    public Type Type => typeof(T);

    /// <summary>
    /// True if the term is a LVar
    /// </summary>
    public bool IsLVar => _lvar != null;
    
    /// <summary>
    /// True if the term has a value
    /// </summary>
    public bool IsValue => _lvar == null;

    LVar ITerm.LVar => LVar;

    /// <summary>
    /// Gets the LVar of the term
    /// </summary>
    public LVar<T> LVar => _lvar!;
    
    /// <summary>
    /// Gets the value of the term
    /// </summary>
    public T Value => _value;
    
    /// <inheritdoc />
    public override string ToString() 
        => _lvar != null ? _lvar!.ToString() : _value!.ToString()!;
    
    
    /// <summary>
    /// Implicitly convert a LVar to a term
    /// </summary>
    public static implicit operator Term<T>(LVar<T> lvar) => new(lvar);
    
    /// <summary>
    /// Implicitly convert a value to a term
    /// </summary>
    public static implicit operator Term<T>(T value) => new(value);
}
