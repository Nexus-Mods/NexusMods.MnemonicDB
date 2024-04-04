using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB;

/// <inheritdoc />
public class CommitResult(IDb db, IDictionary<EntityId, EntityId> remaps) : ICommitResult
{
    /// <inheritdoc />
    public EntityId this[EntityId id] =>
        remaps.TryGetValue(id, out var found) ? found : id;

    public T Remap<T>(T model) where T : struct, IEntity
    {
        return db.Get<T>(remaps[model.Header.Id]);
    }

    /// <inheritdoc />
    public TxId NewTx => db.BasisTxId;
}
