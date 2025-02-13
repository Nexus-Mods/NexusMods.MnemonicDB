using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

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
    ///     Adds a new datom to the transaction
    /// </summary>
    void Add<TVal, TAttribute>(EntityId entityId, TAttribute attribute, TVal val, bool isRetract = false) 
        where TAttribute : IWritableAttribute<TVal>;
    
    /// <summary>
    ///    Adds a new datom to the transaction
    /// </summary>
    void Add<TVal, TLowLevel, TSerializer>(EntityId entityId, Attribute<TVal, TLowLevel, TSerializer> attribute, TVal val, bool isRetract = false)
        where TSerializer : IValueSerializer<TLowLevel>;
    
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
    void Retract<TVal, TAttribute>(EntityId entityId, TAttribute attribute, TVal val)
    where TAttribute : IWritableAttribute<TVal>
        => Add(entityId, attribute, val, isRetract: true);

    /// <summary>
    /// Retract a specific datom
    /// </summary>
    void Add(Datom datom);

    /// <summary>
    /// Tries to find and return a previously attached entity by ID.
    /// </summary>
    bool TryGet<TEntity>(EntityId entityId, [NotNullWhen(true)] out TEntity? entity)
        where TEntity : class, ITemporaryEntity;

    /// <summary>
    ///     Commits the transaction
    /// </summary>
    Task<ICommitResult> Commit();
}
