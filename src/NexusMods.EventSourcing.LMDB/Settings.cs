using NexusMods.Paths;

namespace NexusMods.EventSourcing.LMDB;

public class Settings
{
    public AbsolutePath StorageLocation { get; set; } = default!;
}
