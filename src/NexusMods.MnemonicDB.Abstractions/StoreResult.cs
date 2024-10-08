using System.Collections.Frozen;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// The result of a transaction being commited to a store.
/// </summary>
public class StoreResult
{
    /// <summary>
    /// The assigned transaction id.
    /// </summary>
    public required TxId AssignedTxId { get; init; }

    /// <summary>
    /// The remaps that were created during the transaction.
    /// </summary>
    public required FrozenDictionary<EntityId, EntityId> Remaps { get; init; }

    /// <summary>
    /// The snapshot of the store after the transaction.
    /// </summary>
    public required ISnapshot Snapshot { get; init; }
}
