using System;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions.Models;

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

    /// <summary>
    /// Starts a new transaction.
    /// </summary>
    /// <returns></returns>
    public ITransaction BeginTransaction();

    /// <summary>
    /// A sequential stream of commits to the database.
    /// </summary>
    public IObservable<ICommitResult> Commits { get; }

    /// <summary>
    /// Gets a active read model for the given entity id. Once the entity is read,
    /// future updates to the connection will update the these read models, and fire off
    /// the correct INotifyPropertyChanged events.
    /// </summary>
    public IReadModel GetActive<T>(EntityId result)
    where T : class, IReadModel;
}
