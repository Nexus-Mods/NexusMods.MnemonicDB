using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions;

public class StoreResult
{
    public required TxId AssignedTxId { get; init; }
    public required Dictionary<EntityId, EntityId> Remaps { get; init; }
    public required ISnapshot Snapshot { get; init; }
    public required IReadOnlyCollection<IReadDatom> Datoms { get; init; }
}
