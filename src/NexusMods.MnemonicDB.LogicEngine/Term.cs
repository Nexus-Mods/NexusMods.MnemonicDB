using System;

namespace NexusMods.MnemonicDB.LogicEngine;

public readonly record struct Term<T>
{
    private readonly LVar<T>? _lVar;
    private readonly T? _value;
    
    public Term(LVar<T>? LVar, T? Value)
    {
        _lVar = LVar;
        _value = Value;
    }
    
    public static implicit operator Term<T>(T value) => new(null, value);
    public static implicit operator Term<T>(LVar<T> lVar) => new(lVar, default);

    public bool IsLVar => _lVar != null;
    public bool IsValue => _lVar == null;
    
    /// <summary>
    /// Gets the value of the term if it is a value, otherwise throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public T Value => _value ?? throw new InvalidOperationException("Value is null");
    public LVar<T> LVar => _lVar ?? throw new InvalidOperationException("LVar is null");
}
