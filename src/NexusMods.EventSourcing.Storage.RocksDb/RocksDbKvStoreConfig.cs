using NexusMods.Paths;

namespace NexusMods.EventSourcing.Storage.RocksDb;

public class RocksDbKvStoreConfig
{
    public required AbsolutePath Path { get; init; }
}
