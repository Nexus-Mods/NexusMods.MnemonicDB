﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Represents the low-level storage for datoms.
/// </summary>
public interface IDatomStore : IDisposable
{
    /// <summary>
    ///     An observable of the transaction log, for getting the latest changes to the store. This observable
    /// will always start with the most recent value, so there is no reason to use `StartWith` or `Replay` on it.
    /// </summary>
    public IObservable<IDb> TxLog { get; }

    /// <summary>
    ///     Gets the latest transaction id found in the log.
    /// </summary>
    public TxId AsOfTxId { get; }

    /// <summary>
    /// The Attribute Cache the store is using.
    /// </summary>
    AttributeCache AttributeCache { get; }
    
    /// <summary>
    /// The attribute resolver the store is using.
    /// </summary>
    AttributeResolver AttributeResolver { get; }
    
    /// <summary>
    ///    Transacts (adds) the given datoms into the store.
    /// </summary>
    public (StoreResult, IDb) Transact(Datoms segment);
    
    
    /// <summary>
    ///   Transacts (adds) the given datoms into the store.
    /// </summary>
    public Task<(StoreResult, IDb)> TransactAsync(Datoms segment);
    
    /// <summary>
    ///     Create a snapshot of the current state of the store.
    /// </summary>
    ISnapshot GetSnapshot();
}
