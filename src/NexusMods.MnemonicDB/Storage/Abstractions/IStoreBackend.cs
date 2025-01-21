using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Storage.Abstractions;

/// <summary>
/// A store backend is the backing KV store of a datoms store. It is responsible for
/// sorting, storing and snapshotting spans of data
/// </summary>
public interface IStoreBackend : IDisposable
{
    /// <summary>
    /// Returns the attribute cache for this store, this cache should be shared across
    /// the datom store, the connection, and the db instances
    /// </summary>
    public AttributeCache AttributeCache { get; }
    
    /// <summary>
    /// Create a new write batch
    /// </summary>
    public IWriteBatch CreateBatch();

    /// <summary>
    /// Initialize the store backend with the given location
    /// </summary>
    public void Init(AbsolutePath location);

    /// <summary>
    ///     Gets a snapshot of the current state of the store that will not change
    ///     during calls to GetIterator
    /// </summary>
    public ISnapshot GetSnapshot();

    /// <summary>
    /// Flushes all the logs to disk, and performs a compaction, recommended if you want to archive the database
    /// and move it somewhere else.
    /// </summary>
    public void FlushAndCompact();
}
