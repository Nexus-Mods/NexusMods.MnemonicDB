using NexusMods.Paths;

namespace NexusMods.EventSourcing.RocksDB;

public class Settings
{
    public AbsolutePath StorageLocation { get; set; } = default!;
}
