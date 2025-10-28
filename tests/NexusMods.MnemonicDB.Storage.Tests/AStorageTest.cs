using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.Storage.Tests.TestAttributes;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Storage.Tests;

public abstract class AStorageTest : IDisposable
{
    private readonly AbsolutePath _path;
    private readonly IServiceProvider _provider;
    protected readonly DatomStoreSettings DatomStoreSettings;
    protected AttributeCache AttributeCache => DatomStore.AttributeCache;
    protected AttributeResolver AttributeResolver => Connection.AttributeResolver;

    protected readonly ILogger Logger;

    private ulong _tempId = 1;
    protected IDatomStore DatomStore => Connection.DatomStore;

    protected AStorageTest(IServiceProvider provider, bool isInMemory)
    {
        _provider = provider;
        
        _path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("tests_datomstore" + Guid.NewGuid());

        DatomStoreSettings = new DatomStoreSettings
        {
            Path = isInMemory ? null : _path,
        };
        
        Connection = provider.GetRequiredService<IConnectionFactory>().Create(provider, DatomStoreSettings);
        Logger = provider.GetRequiredService<ILogger<AStorageTest>>();
    }

    public IConnection Connection { get; set; }

    public void Dispose()
    {
        Connection.Dispose();
    }

    public EntityId NextTempId()
    {
        var partition = PartitionId.Entity;
        var id = Interlocked.Increment(ref _tempId);
        id |= (ulong)partition << 40;
        return PartitionId.Temp.MakeEntityId(id);
    }
}
