﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Traits;
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
        where TSerializer : IValueSerializer<TLowLevel> where TVal : notnull where TLowLevel : notnull;
    
    /// <summary>
    ///     Adds datoms for adding the given ids to the transaction under the given attribute
    /// </summary>
    void Add(EntityId entityId, ReferencesAttribute attribute, IEnumerable<EntityId> ids);
    
    /// <summary>
    /// Adds a new datom using spans for the 
    /// </summary>
    void Add(EntityId e, AttributeId a, ValueTag valueTag, ReadOnlySpan<byte> valueSpan, bool isRetract = false);

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
    /// Retracts all datoms for the given attribute for the given entity as seen by the given db. If none are found,
    /// nothing happens
    /// </summary>
    void RetractAll(IDb db, EntityId entityId, IAttribute attribute)
    {
        var ent = db.Get(entityId);
        var aid = db.AttributeCache.GetAttributeId(attribute.Id);
        var range = ent.GetRange(aid);

        for (var idx = range.Start.Value; idx < range.End.Value; idx++)
        {
            throw new NotImplementedException();
            //var span = ent.GetValueSpan(idx, out var valueTag);
            //Add(entityId, aid, valueTag, span, isRetract: true);
        }
    }
    
    /// <summary>
    /// Retract a specific datom
    /// </summary>
    void Add(Datom datom);
    
    void Add(IDatomLikeRO datom)
    {
        throw new NotSupportedException();
    }
    
    /// <summary>
    /// Tries to find and return a previously attached entity by ID.
    /// </summary>
    bool TryGet<TEntity>(EntityId entityId, [NotNullWhen(true)] out TEntity? entity)
        where TEntity : class, ITemporaryEntity;

    /// <summary>
    /// Creates a sub-transaction.
    /// </summary>
    ISubTransaction CreateSubTransaction();

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
