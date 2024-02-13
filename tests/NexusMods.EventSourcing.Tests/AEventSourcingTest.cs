using Microsoft.Extensions.Logging.Abstractions;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.DatomStore;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Tests;

public class AEventSourcingTest : IDisposable
{
    private readonly AbsolutePath _tmpPath;
    private readonly AttributeRegistry _registry;
    protected readonly RocksDBDatomStore Store;
    protected readonly Connection Connection;

    protected AEventSourcingTest(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    {
        _tmpPath = FileSystem.Shared.GetKnownPath(KnownPath.TempDirectory).Combine(Guid.NewGuid() + ".rocksdb");
        var dbSettings = new DatomStoreSettings()
        {
            Path = _tmpPath,
        };
        _registry = new AttributeRegistry(valueSerializers, attributes);
        Store = new RocksDBDatomStore(new NullLogger<RocksDBDatomStore>(), _registry, dbSettings);
        Connection = new Connection(Store, attributes, valueSerializers);
    }

    public void Dispose()
    {
        Store.Dispose();
        _tmpPath.DeleteDirectory();
    }
}
