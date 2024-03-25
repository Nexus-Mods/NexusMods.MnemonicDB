using NexusMods.EventSourcing.Storage.Abstractions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.RocksDbBackend;

public class IndexStore(ColumnFamilyHandle handle) : IIndexStore
{
    public ColumnFamilyHandle Handle => handle;

}
