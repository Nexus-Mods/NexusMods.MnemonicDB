using NexusMods.Paths;

namespace NexusMods.EventSourcing.DatomStore;

public class DatomStoreSettings
{
    /// <summary>
    /// The path to the directory where the RocksDB database will be stored.
    /// </summary>
    public AbsolutePath Path { get; set; }
}
