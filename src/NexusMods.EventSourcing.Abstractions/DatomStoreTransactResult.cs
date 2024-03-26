using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Result of a transact operation, contains the new transaction id and any entity remaps that were performed
/// during the transaction.
/// </summary>
public record DatomStoreTransactResult(
    TxId TxId,
    RefCountDisposable<ISnapshot> refCountedSnapshot,
    Dictionary<EntityId, EntityId> Remaps)
{
    public IDb Db
    {
        get
        {
            refCountedSnapshot.AddRef();
            return new Db(refCountedSnapshot, TxId);
        }
    }
}
