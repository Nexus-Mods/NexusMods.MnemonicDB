using System;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Represents a transaction, which is a set of proposed changes to the datom store
/// </summary>
public interface ITransaction : IDisposable
{
    /// <summary>
    ///     Gets the temporary id for the transaction
    /// </summary>
    public TxId ThisTxId { get; }

    /// <summary>
    ///     Gets a temporary id for a new entity
    /// </summary>
    /// <returns></returns>
    EntityId TempId();

    /// <summary>
    ///     Adds a new datom to the transaction
    /// </summary>
    void Add<TVal, TLowLevel>(EntityId entityId, Attribute<TVal, TLowLevel> attribute, TVal val);

    /// <summary>
    ///     Commits the transaction
    /// </summary>
    Task<ICommitResult> Commit();
}
