using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Traits;

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
    /// Exports the database (including all indexes) to the given stream
    /// </summary>
    public Task ExportAsync(Stream stream);
    
    /// <summary>
    /// Imports the database (including all indexes) from the given stream.
    /// Any existing data will be deleted before importing.
    /// </summary>
    public Task ImportAsync(Stream stream);
    
    /// <summary>
    ///    Transacts (adds) the given datoms into the store.
    /// </summary>
    public (StoreResult, IDb) Transact(DatomList segment);
    
    
    /// <summary>
    ///   Transacts (adds) the given datoms into the store.
    /// </summary>
    public Task<(StoreResult, IDb)> TransactAsync(DatomList segment);
    
    /// <summary>
    ///     Create a snapshot of the current state of the store.
    /// </summary>
    ISnapshot GetSnapshot();
}
