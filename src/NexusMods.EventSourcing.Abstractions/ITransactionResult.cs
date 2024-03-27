using System;
using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
///     The result of a transaction commit, contains metadata useful for looking up the results of the transaction
/// </summary>
public interface ITransactionResult
{
    /// <summary>
    ///     The new transaction id after the commit
    /// </summary>
    public TxId NewTx { get; }

    /// <summary>
    ///     The datoms that were added to the store as a result of the transaction
    /// </summary>
    public IReadOnlyCollection<IReadDatom> Added { get; }

    /// <summary>
    ///     The time it took to commit the transaction
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    ///     Gets a fresh reference to the database after the transaction, this should be disposed when done.
    /// </summary>
    /// <returns></returns>
    public IDb NewDb();
}
