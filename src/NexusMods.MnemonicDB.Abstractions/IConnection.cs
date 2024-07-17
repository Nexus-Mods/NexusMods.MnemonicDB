using System;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// A database revision, which includes a datom and the datoms added to it.
/// </summary>
public struct Revision
{
    /// <summary>
    /// The database for the most recent transaction
    /// </summary>
    public IDb Database;

    /// <summary>
    /// The datoms that were added in the most recent transaction
    /// </summary>
    public IndexSegment AddedDatoms;
}

/// <summary>
///     Represents a connection to a database.
/// </summary>
public interface IConnection
{
    /// <summary>
    ///     Gets the current database.
    /// </summary>
    public IDb Db { get; }

    /// <summary>
    /// The attribute registry for this connection
    /// </summary>
    public IAttributeRegistry Registry { get; }

    /// <summary>
    ///     Gets the most recent transaction id.
    /// </summary>
    public TxId TxId { get; }

    /// <summary>
    ///     A sequential stream of database revisions.
    /// </summary>
    public IObservable<IDb> Revisions { get; }

    /// <summary>
    /// A service provider that entities can use to resolve their values
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Returns a snapshot of the database as of the given transaction id.
    /// </summary>
    public IDb AsOf(TxId txId);

    /// <summary>
    ///     Starts a new transaction.
    /// </summary>
    /// <returns></returns>
    public ITransaction BeginTransaction();
}
