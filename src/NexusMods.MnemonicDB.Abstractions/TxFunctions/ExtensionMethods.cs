using System;

namespace NexusMods.MnemonicDB.Abstractions.TxFunctions;

/// <summary>
/// Extension methods for TxFunctions.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Adds a function to the transaction as a TxFunction
    /// </summary>
    public static void Add<T>(this ITransaction tx, T arg, Action<ITransaction, IDb, T> fn) =>
        tx.Add(new TxFunction<T>(fn, arg));

    /// <summary>
    /// Adds a function to the transaction as a TxFunction
    /// </summary>
    public static void Add<TA, TB>(this ITransaction tx, TA a, TB b, Action<ITransaction, IDb, TA, TB> fn) =>
        tx.Add(new TxFunction<TA, TB>(fn, a, b));
}
