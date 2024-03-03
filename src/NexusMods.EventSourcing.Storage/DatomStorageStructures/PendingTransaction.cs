using System.Collections.Generic;
using System.Threading.Tasks;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.DatomStorageStructures;

/// <summary>
/// Information about a pending transaction, and a way to signal its completion.
/// </summary>
internal class PendingTransaction
{
    /// <summary>
    /// A completion source for the transaction, resolves when the transaction is commited to the
    /// transaction log and available to readers.
    /// </summary>
    public TaskCompletionSource<TxId> CompletionSource { get; } = new();

    /// <summary>
    /// Entity IDs that are remapped in the transaction
    /// </summary>
    public Dictionary<EntityId, EntityId> Remaps { get; } = new();

    /// <summary>
    /// The data to be commited
    /// </summary>
    public required IWriteDatom[] Data { get; init; }

    /// <summary>
    /// The transaction ID that was assigned to the transaction when it was commited
    /// </summary>
    public TxId? AssignedTxId { get; set; }
}
