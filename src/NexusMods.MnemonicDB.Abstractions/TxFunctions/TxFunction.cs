using System;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.TxFunctions;

/// <summary>
/// A function that can be applied to a transaction, that takes a single argument.
/// </summary>
public record TxFunction<T>(Action<ITransaction, IDb, T> Fn, T val) : ITxFunction
{
    /// <inheritdoc />
    public void Apply(ITransaction tx, IDb basis) => Fn(tx, basis, val);

    /// <inheritdoc />
    public virtual bool Equals(ITxFunction? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return other is TxFunction<T> function && Equals(function);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Fn, val);
    }
}


/// <summary>
/// A function that can be applied to a transaction, that takes a single argument.
/// </summary>
public record TxFunction<TA, TB>(Action<ITransaction, IDb, TA, TB> Fn, TA A, TB B) : ITxFunction
{
    /// <inheritdoc />
    public void Apply(ITransaction tx, IDb basis) => Fn(tx, basis, A, B);

    /// <inheritdoc />
    public virtual bool Equals(ITxFunction? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return other is TxFunction<TA, TB> function && Equals(function);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Fn, A, B);
    }
}
