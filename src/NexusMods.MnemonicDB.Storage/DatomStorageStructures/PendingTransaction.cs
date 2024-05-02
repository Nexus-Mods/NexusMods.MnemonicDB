using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.MnemonicDB.Storage.DatomStorageStructures;

/// <summary>
///     Information about a pending transaction, and a way to signal its completion.
/// </summary>
internal class PendingTransaction
{
    /// <summary>
    ///     A completion source for the transaction, resolves when the transaction is commited to the
    ///     transaction log and available to readers.
    /// </summary>
    public TaskCompletionSource<StoreResult> CompletionSource { get; } = new();

    /// <summary>
    ///     The data to be commited
    /// </summary>
    public required IndexSegment Data { get; init; }

    /// <summary>
    ///     Tx functions to be applied to the transaction, if any
    /// </summary>
    public required HashSet<ITxFunction>? TxFunctions { get; init; }

    /// <summary>
    ///     A function for creating a new database instance from a given snapshot. Not required
    /// if TxFunctions is null.
    /// </summary>
    public required Func<ISnapshot, IDb>? DatabaseFactory { get; init; }
}
