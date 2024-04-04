using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.Storage.Serializers;
using NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes;
using NexusMods.MnemonicDB.TestModel.ValueSerializers;
using NexusMods.Paths;
using FileAttributes = NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MnemonicDB.Storage.Tests;

public abstract class AStorageTest : IAsyncLifetime
{
    private readonly AbsolutePath _path;
    private readonly IServiceProvider _provider;
    protected readonly DatomStoreSettings DatomStoreSettings;

    protected readonly ILogger Logger;
    protected readonly AttributeRegistry Registry;

    private ulong _tempId = 1;
    protected IDatomStore DatomStore;

    protected AStorageTest(IServiceProvider provider, Func<AttributeRegistry, IStoreBackend>? backendFn = null)
    {
        _provider = provider;
        Registry = new AttributeRegistry(provider.GetRequiredService<IEnumerable<IValueSerializer>>(),
            provider.GetRequiredService<IEnumerable<IAttribute>>());
        Registry.Populate([
            new DbAttribute(Symbol.Intern<ModAttributes.Name>(), AttributeId.From(10), Symbol.Intern<SizeSerializer>()),
            new DbAttribute(Symbol.Intern<FileAttributes.Path>(), AttributeId.From(20),
                Symbol.Intern<RelativePathSerializer>()),
            new DbAttribute(Symbol.Intern<FileAttributes.Hash>(), AttributeId.From(21),
                Symbol.Intern<HashSerializer>()),
            new DbAttribute(Symbol.Intern<FileAttributes.Size>(), AttributeId.From(22),
                Symbol.Intern<SizeSerializer>()),
            new DbAttribute(Symbol.Intern<FileAttributes.ModId>(), AttributeId.From(23),
                Symbol.Intern<EntityIdSerializer>()),
            new DbAttribute(Symbol.Intern<ModAttributes.Name>(), AttributeId.From(24),
                Symbol.Intern<StringSerializer>()),
            new DbAttribute(Symbol.Intern<ModAttributes.LoadoutId>(), AttributeId.From(25),
                Symbol.Intern<EntityIdSerializer>()),
            new DbAttribute(Symbol.Intern<LoadoutAttributes.Name>(), AttributeId.From(26),
                Symbol.Intern<StringSerializer>()),
            new DbAttribute(Symbol.Intern<CollectionAttributes.Name>(), AttributeId.From(27),
                Symbol.Intern<StringSerializer>()),
            new DbAttribute(Symbol.Intern<CollectionAttributes.LoadoutId>(), AttributeId.From(28),
                Symbol.Intern<EntityIdSerializer>()),
            new DbAttribute(Symbol.Intern<CollectionAttributes.Mods>(), AttributeId.From(29),
                Symbol.Intern<EntityIdSerializer>())
        ]);
        _path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("tests_datomstore" + Guid.NewGuid());

        DatomStoreSettings = new DatomStoreSettings
        {
            Path = _path
        };

        backendFn ??= registry => new Backend(registry);


        DatomStore = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), Registry, DatomStoreSettings,
            backendFn(Registry));

        Logger = provider.GetRequiredService<ILogger<AStorageTest>>();
    }

    public async Task InitializeAsync()
    {
        await DatomStore.Sync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public EntityId NextTempId()
    {
        var id = Interlocked.Increment(ref _tempId);
        return EntityId.From(Ids.MakeId(Ids.Partition.Tmp, id));
    }
}
