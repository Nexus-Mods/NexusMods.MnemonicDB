using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// Starts a new transaction.
    /// </summary>
    /// <returns></returns>
    public ITransaction BeginTransaction();

    /// <summary>
    /// A sequential stream of commits to the database.
    /// </summary>
    public IObservable<(TxId TxId, Datom Datoms)> Commits { get; }

    /// <summary>
    /// Gets the active read model for the given entity id, this entity will
    /// automatically update as new commits are made to the database that modify
    /// its state. It will update via INotifyPropertyChanged.
    /// </summary>
    public T GetActive<T>(EntityId id) where T : IActiveReadModel;
}
