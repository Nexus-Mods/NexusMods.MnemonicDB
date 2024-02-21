using Microsoft.Extensions.Logging.Abstractions;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.DatomStore;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Tests;

public class AEventSourcingTest : IDisposable
{
    private readonly AbsolutePath _tmpPath;
    protected readonly RocksDBDatomStore Store;
    protected readonly Connection Connection;

    protected AEventSourcingTest(IEnumerable<IValueSerializer> valueSerializers,
        IEnumerable<IAttribute> attributes)
    {
        _tmpPath = FileSystem.Shared.GetKnownPath(KnownPath.TempDirectory).Combine(Guid.NewGuid() + ".rocksdb");
        var dbSettings = new DatomStoreSettings()
        {
            Path = _tmpPath,
        };
        var valueSerializerArray = valueSerializers.ToArray();

        var attributeArray = attributes.ToArray();
        var registry = new AttributeRegistry(valueSerializerArray, attributeArray);
        Store = new RocksDBDatomStore(new NullLogger<RocksDBDatomStore>(), registry, dbSettings);
        Connection = new Connection(Store, attributeArray, valueSerializerArray);
    }

    public void Dispose()
    {
        Store.Dispose();
        _tmpPath.DeleteDirectory();
    }
}
