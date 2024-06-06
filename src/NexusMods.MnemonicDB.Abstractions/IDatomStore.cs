using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

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
    public IObservable<(TxId TxId, ISnapshot Snapshot)> TxLog { get; }

    /// <summary>
    ///     Gets the latest transaction id found in the log.
    /// </summary>
    public TxId AsOfTxId { get; }

    /// <summary>
    /// The Attribute Registry for this store
    /// </summary>
    IAttributeRegistry Registry { get; }

    /// <summary>
    ///     Transacts (adds) the given datoms into the store.
    /// </summary>
    public Task<StoreResult> Transact(IndexSegment datoms, HashSet<ITxFunction>? txFunctions = null, Func<ISnapshot, IDb>? databaseFactory = null);

    /// <summary>
    /// Executes an empty transaction. Returns a StoreResult valid asof the latest
    /// transaction
    /// </summary>
    /// <returns></returns>
    public Task<StoreResult> Sync();

    /// <summary>
    ///     Registers new attributes with the store. These should already have been transacted into the store.
    /// </summary>
    /// <param name="newAttrs"></param>
    Task RegisterAttributes(IEnumerable<DbAttribute> newAttrs);

    /// <summary>
    ///     Create a snapshot of the current state of the store.
    /// </summary>
    ISnapshot GetSnapshot();

    /// <summary>
    /// Starts the store, this is idempotent so calling it multiple times is safe
    /// </summary>
    Task StartAsync(CancellationToken none);
}
