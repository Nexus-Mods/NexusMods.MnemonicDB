using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

/// <inheritdoc />
public class CommitResult(TxId newTxId, IDictionary<ulong, ulong> remaps, IDatom[] datoms) : ICommitResult
{
    /// <inheritdoc />
    public EntityId this[EntityId id] =>
        remaps.TryGetValue(id.Value, out var found) ? EntityId.From(found) : id;

    /// <inheritdoc />
    public TxId NewTx => newTxId;

    /// <inheritdoc />
    public IEnumerable<IDatom> Datoms => datoms;
}
