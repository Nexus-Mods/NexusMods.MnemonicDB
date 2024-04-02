using System;
using NexusMods.MneumonicDB.Abstractions.DatomComparators;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Abstractions;

/// <summary>
///     Represents a snapshot of the database at a specific point of time. Snapshots are immutable
///     and do not live past the life of the application, or after the IDisposable.Dispose method is called.
///     Using snapshots to query the database is the most efficient way, and is leveraged by the IDb interface,
///     to provide a read-only view of the database.
/// </summary>
public interface ISnapshot
{
    /// <summary>
    ///     Gets an iterator for the given index type, if historical is true, the current and history
    /// indexes are merged into a single iterator.
    /// </summary>
    IDatomSource GetIterator(IndexType type, bool historical = false);
}
