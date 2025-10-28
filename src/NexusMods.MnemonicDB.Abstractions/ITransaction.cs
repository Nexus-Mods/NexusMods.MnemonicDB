using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Represents a transaction, which is a set of proposed changes to the datom store
/// </summary>
[PublicAPI]
public interface ITransaction : IDisposable
{
    /// <summary>
    ///     Gets the temporary id for the transaction
    /// </summary>
    public TxId ThisTxId { get; }
    
    /// <summary>
    ///     Gets a temporary id for a new entity in the given partition
    /// </summary>
    /// <returns></returns>
    EntityId TempId(PartitionId partition);

    /// <summary>
    ///     Gets a temporary id for a new entity in the default (Entity) partition
    /// </summary>
    /// <returns></returns>
    EntityId TempId();
    

    
    /// <summary>
    /// Tries to find and return a previously attached entity by ID.
    /// </summary>
    bool TryGet<TEntity>(EntityId entityId, [NotNullWhen(true)] out TEntity? entity)
        where TEntity : class, ITemporaryEntity
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Creates a sub-transaction.
    /// </summary>
    SubTransaction CreateSubTransaction();

    /// <summary>
    /// Resets the transaction.
    /// </summary>
    void Reset();
}

/// <summary>
/// Represents a transaction that can be committed to the DB.
/// </summary>
[PublicAPI]
public interface IMainTransaction : ITransaction
{
    /// <summary>
    /// Commits the transaction
    /// </summary>
    Task<ICommitResult> Commit();
}

/// <summary>
/// A sub-transaction.
/// </summary>
[PublicAPI]
public interface ISubTransaction : ITransaction
{
    /// <summary>
    /// Commits the data to the parent transaction.
    /// </summary>
    void CommitToParent();
}
