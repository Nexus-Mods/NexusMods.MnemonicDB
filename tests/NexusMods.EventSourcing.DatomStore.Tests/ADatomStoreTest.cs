using Microsoft.Extensions.Logging.Abstractions;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.Paths;
// ReSharper disable PossibleMultipleEnumeration

namespace NexusMods.EventSourcing.DatomStore.Tests;

public abstract class ADatomStoreTest : IDisposable
{
    private readonly AbsolutePath _tmpPath;
    protected readonly RocksDBDatomStore Store;
    protected readonly Connection Connection;

    protected ADatomStoreTest(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    {
        _tmpPath = FileSystem.Shared.GetKnownPath(KnownPath.TempDirectory).Combine(Guid.NewGuid() + ".rocksdb");
        var dbSettings = new DatomStoreSettings()
        {
            Path = _tmpPath,
        };
        var registry = new AttributeRegistry(valueSerializers, attributes);
        Store = new RocksDBDatomStore(new NullLogger<RocksDBDatomStore>(), registry, dbSettings);
        Connection = new Connection(Store, attributes, valueSerializers);
    }

    public void Dispose()
    {
        Store.Dispose();
        _tmpPath.DeleteDirectory();
    }
}
