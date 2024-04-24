using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     The result of a transaction commit, contains metadata useful for looking up the results of the transaction
/// </summary>
public interface ICommitResult
{
    /// <summary>
    ///     Remaps a temporary id to a permanent id, or returns the original id if it was not a temporary id
    /// </summary>
    /// <param name="id"></param>
    public EntityId this[EntityId id] { get; }


    /// <summary>
    ///   Remaps a ReadModel to a new instance with the new ids, if the entity is not new, it
    /// updates the entity anyway to be current as of the commit
    /// </summary>
    public T Remap<T>(T model) where T : IEntity;

    /// <summary>
    ///     Gets the new TxId after the commit
    /// </summary>
    public TxId NewTx { get; }

    /// <summary>
    /// The database up-to-date with the new transaction
    /// </summary>
    public IDb Db { get; }
}
