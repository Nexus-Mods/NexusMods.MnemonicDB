using System.Collections.Generic;

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
    public ICommitResult Transact(IEnumerable<IDatom> datoms);
}
