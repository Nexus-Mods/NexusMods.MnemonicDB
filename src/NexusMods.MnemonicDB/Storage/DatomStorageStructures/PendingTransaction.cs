﻿using System;
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
    public TaskCompletionSource<(StoreResult, IDb)> CompletionSource { get; } = new();

    /// <summary>
    ///     The data to be commited
    /// </summary>
    public required IndexSegment Data { get; set; }

    /// <summary>
    ///     Tx functions to be applied to the transaction, if any
    /// </summary>
    public required HashSet<ITxFunction>? TxFunctions { get; init; }
    
    public void Complete(StoreResult result, IDb db)
    {
        Data = new IndexSegment();
        Task.Run(() => CompletionSource.SetResult((result, db)));
    }
}
