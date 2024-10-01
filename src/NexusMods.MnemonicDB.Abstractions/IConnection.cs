﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// The associated attribute resolver.
    /// </summary>
    AttributeResolver AttributeResolver { get; }
    
    /// <summary>
    /// The attribute cache for this connection.
    /// </summary>
    AttributeCache AttributeCache { get; }

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
    /// Returns a snapshot of the database that contains all current and historical datoms.
    /// </summary>
    public IDb History();

    /// <summary>
    ///     Starts a new transaction.
    /// </summary>
    /// <returns></returns>
    public ITransaction BeginTransaction();
    
    /// <summary>
    /// The analyzers that are available for this connection
    /// </summary>
    public IAnalyzer[] Analyzers { get; }
    
    /// <summary>
    /// Deletes the entities with the given ids, also deleting them from any historical indexes. Returns the total number
    /// of datoms that were excised.
    /// </summary>
    public Task<ulong> Excise(EntityId[] entityIds);
}
