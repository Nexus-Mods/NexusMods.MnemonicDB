namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Represents a snapshot of the database at a specific point of time. Snapshots are immutable
///     and do not live past the life of the application, or after the IDisposable.Dispose method is called.
///     Using snapshots to query the database is the most efficient way, and is leveraged by the IDb interface,
///     to provide a read-only view of the database.
/// </summary>
public interface ISnapshot : IDatomsIndex
{
    /// <summary>
    /// Construct a new DB with this snapshot and the given parameters, may feel backwards to create DBs this way, but it's so that
    /// the low level iterator types can be injected into the DBs.
    /// </summary>
    public IDb MakeDb(TxId txId, AttributeResolver attributeCache, IConnection? connection = null);

    /// <summary>
    /// Get the max id currently used in the given partition, returns false if not found
    /// </summary>
    public bool TryGetMaxIdInPartition(PartitionId partitionId, out EntityId id);

    /// <summary>
    /// Create a new snapshot that acts as if the given datoms were already transacted into the store. No changes
    /// will be made to the underlying datastore.
    /// </summary>
    public ISnapshot AsIf(Datoms datoms);
}


