using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

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
    public IDb MakeDb(TxId txId, AttributeCache attributeCache, IConnection? connection = null, object? newCache = null, IndexSegment? recentlyAdded = null);
}

/// <summary>
/// A snapshot that returns a specific type of low-level iterator.
/// </summary>
public interface ISnapshot<out TLowLevelIterator> : ISnapshot
    where TLowLevelIterator : ILowLevelIterator
{
    /// <summary>
    /// Get a low-level iterator for this snapshot, this can be combined with slice descriptors to get high performance
    /// access to a portion of the index
    /// </summary>
    [MustDisposeResource]
    public TLowLevelIterator GetLowLevelIterator();
}
