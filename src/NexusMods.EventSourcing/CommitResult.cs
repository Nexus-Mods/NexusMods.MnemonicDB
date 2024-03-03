using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

/// <inheritdoc />
public class CommitResult(TxId newTxId, IDictionary<EntityId, EntityId> remaps) : ICommitResult
{
    /// <inheritdoc />
    public EntityId this[EntityId id] =>
        remaps.TryGetValue(id, out var found) ? found : id;

    /// <inheritdoc />
    public TxId NewTx => newTxId;
}
