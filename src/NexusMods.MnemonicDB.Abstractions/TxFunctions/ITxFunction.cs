using System;

namespace NexusMods.MnemonicDB.Abstractions.TxFunctions;

/// <summary>
/// Defines a transactor function. These are functions that are applied inside the guts
/// of the `Log` function of the `DatomStore`. They are executed serially and are used
/// to maintain consistency of the database when absolutely require. They are not designed
/// for general use and should be used sparingly as they are essentially single-threaded
/// </summary>
public interface ITxFunction
{
    /// <summary>
    /// Tells the function to add datoms to the transaction. The most recent copy of the database
    /// is provided as a basis for the function to work on. Functions cannot see the results of
    /// other functions in the same transaction.
    /// </summary>
    public void Apply(Transaction tx);
}
