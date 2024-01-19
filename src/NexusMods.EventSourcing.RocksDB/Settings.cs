using NexusMods.Paths;

namespace NexusMods.EventSourcing.RocksDB;

/// <summary>
/// Settings for the RocksDB event store.
/// </summary>
public class Settings
{
    /// <summary>
    /// The storage location for the RocksDB database
    /// </summary>
    public AbsolutePath StorageLocation { get; set; } = default!;
}
