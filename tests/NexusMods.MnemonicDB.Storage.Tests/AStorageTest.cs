using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.Storage.Serializers;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.MnemonicDB.TestModel.ValueSerializers;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;

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
            new DbAttribute(File.Path.Id, AttributeId.From(20),
                Symbol.Intern<RelativePathSerializer>()),
            new DbAttribute(File.Hash.Id, AttributeId.From(21),
                Symbol.Intern<HashSerializer>()),
            new DbAttribute(File.Size.Id, AttributeId.From(22),
                Symbol.Intern<SizeSerializer>()),
            new DbAttribute(File.ModId.Id, AttributeId.From(23),
                Symbol.Intern<EntityIdSerializer>()),
            new DbAttribute(Mod.Name.Id, AttributeId.From(24),
                Symbol.Intern<StringSerializer>()),
            new DbAttribute(Mod.LoadoutId.Id, AttributeId.From(25),
                Symbol.Intern<EntityIdSerializer>()),
            new DbAttribute(Loadout.Name.Id, AttributeId.From(26),
                Symbol.Intern<StringSerializer>()),
            new DbAttribute(Collection.Name.Id, AttributeId.From(27),
                Symbol.Intern<StringSerializer>()),
            new DbAttribute(Collection.Loadout.Id, AttributeId.From(28),
                Symbol.Intern<EntityIdSerializer>()),
            new DbAttribute(Collection.Mods.Id, AttributeId.From(29),
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
        Registry.Dispose();
        return Task.CompletedTask;
    }

    public EntityId NextTempId()
    {
        var id = Interlocked.Increment(ref _tempId);
        return EntityId.From(Ids.MakeId(Ids.Partition.Tmp, id));
    }
}
