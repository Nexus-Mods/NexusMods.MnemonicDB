using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;

namespace NexusMods.EventSourcing;

/// <inheritdoc />
public class CommitResult(IDb db, IDictionary<EntityId, EntityId> remaps) : ICommitResult
{
    /// <inheritdoc />
    public EntityId this[EntityId id] =>
        remaps.TryGetValue(id, out var found) ? found : id;

    public T Remap<T>(T model) where T : IReadModel
    {
        return db.Get<T>(remaps[model.Id]);
    }

    /// <inheritdoc />
    public TxId NewTx => db.BasisTxId;
}
