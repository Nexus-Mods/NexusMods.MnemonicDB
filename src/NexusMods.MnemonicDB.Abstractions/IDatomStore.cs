using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Represents the low-level storage for datoms.
/// </summary>
public interface IDatomStore : IDisposable
{
    /// <summary>
    ///     An observable of the transaction log, for getting the latest changes to the store.
    /// </summary>
    public IObservable<(TxId TxId, ISnapshot Snapshot)> TxLog { get; }

    /// <summary>
    ///     Gets the latest transaction id found in the log.
    /// </summary>
    public TxId AsOfTxId { get; }

    IAttributeRegistry Registry { get; }

    /// <summary>
    ///     Transacts (adds) the given datoms into the store.
    /// </summary>
    public Task<StoreResult> Transact(IndexSegment datoms);

    /// <summary>
    ///     Registers new attributes with the store. These should already have been transacted into the store.
    /// </summary>
    /// <param name="newAttrs"></param>
    Task RegisterAttributes(IEnumerable<DbAttribute> newAttrs);

    /// <summary>
    ///     Create a snapshot of the current state of the store.
    /// </summary>
    ISnapshot GetSnapshot();
}
