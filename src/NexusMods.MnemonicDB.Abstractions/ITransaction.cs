using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

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
    EntityId TempId(byte partition = (byte)Ids.Partition.Entity);

    /// <summary>
    ///     Adds a new datom to the transaction
    /// </summary>
    void Add<TVal, TLowLevel>(EntityId entityId, Attribute<TVal, TLowLevel> attribute, TVal val, bool isRetract = false);


    /// <summary>
    ///     Adds datoms for adding the given ids to the transaction under the given attribute
    /// </summary>
    void Add(EntityId entityId, ReferencesAttribute attribute, IEnumerable<EntityId> ids);

    /// <summary>
    /// Adds a transactor function to the transaction
    /// </summary>
    /// <param name="fn"></param>
    void Add(ITxFunction fn);

    /// <summary>
    /// Attach a temporary entity to the transaction, when this transaction is commited,
    /// the entity's `AddTo` method will be called.
    /// </summary>
    /// <param name="entity"></param>
    void Attach(ITemporaryEntity entity);

    /// <summary>
    ///     Adds a new datom to the transaction, that retracts the value for the given attribute
    /// </summary>
    void Retract<TVal, TLowLevel>(EntityId entityId, Attribute<TVal, TLowLevel> attribute, TVal val)
        => Add(entityId, attribute, val, isRetract: true);

    /// <summary>
    ///     Commits the transaction
    /// </summary>
    Task<ICommitResult> Commit();
}
