using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Represents a connection to a database.
/// </summary>
public interface IConnection
{
    /// <summary>
    /// Gets the current database.
    /// </summary>
    public IDb Db { get; }

    /// <summary>
    /// Gets the most recent transaction id.
    /// </summary>
    public TxId TxId { get; }

    /// <summary>
    /// Commits a transaction to the database, and returns the result.
    /// </summary>
    /// <param name="datoms"></param>
    /// <returns></returns>
    public Task<ICommitResult> Transact(IEnumerable<Datom> datoms);

    /// <summary>
    /// Starts a new transaction.
    /// </summary>
    /// <returns></returns>
    public ITransaction BeginTransaction();

    /// <summary>
    /// A sequential stream of commits to the database.
    /// </summary>
    public IObservable<ICommitResult> Commits { get; }
}
