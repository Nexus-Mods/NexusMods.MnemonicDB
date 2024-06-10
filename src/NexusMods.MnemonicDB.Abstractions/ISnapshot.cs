using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Represents a snapshot of the database at a specific point of time. Snapshots are immutable
///     and do not live past the life of the application, or after the IDisposable.Dispose method is called.
///     Using snapshots to query the database is the most efficient way, and is leveraged by the IDb interface,
///     to provide a read-only view of the database.
/// </summary>
public interface ISnapshot
{
    /// <summary>
    /// Get the data specified by the given descriptor as a single segment.
    /// </summary>
    IndexSegment Datoms(SliceDescriptor descriptor);

    /// <summary>
    /// Get the data specified by the given descriptor chunked into segments of datoms of the given size.
    /// </summary>
    IEnumerable<IndexSegment> DatomsChunked(SliceDescriptor descriptor, int chunkSize);

}
