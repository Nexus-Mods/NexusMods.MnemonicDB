using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

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
}
