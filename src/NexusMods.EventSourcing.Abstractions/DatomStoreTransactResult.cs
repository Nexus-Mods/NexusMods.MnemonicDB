using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NexusMods.EventSourcing.Abstractions;

public record DatomStoreTransactResult(TxId TxId, Dictionary<EntityId, EntityId> Remaps);
