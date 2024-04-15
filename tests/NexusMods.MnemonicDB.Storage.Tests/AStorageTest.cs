using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.TestModel;
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
        Registry = new AttributeRegistry(provider.GetRequiredService<IEnumerable<IAttribute>>());
        Registry.Populate([
            new DbAttribute(File.Path.Id, AttributeId.From(20), ValueTags.Utf8Insensitive),
            new DbAttribute(File.Hash.Id, AttributeId.From(21), ValueTags.UInt64),
            new DbAttribute(File.Size.Id, AttributeId.From(22), ValueTags.UInt64),
            new DbAttribute(File.ModId.Id, AttributeId.From(23), ValueTags.Reference),
            new DbAttribute(Mod.Name.Id, AttributeId.From(24), ValueTags.Utf8),
            new DbAttribute(Mod.LoadoutId.Id, AttributeId.From(25), ValueTags.Reference),
            new DbAttribute(Loadout.Name.Id, AttributeId.From(26), ValueTags.Utf8),
            new DbAttribute(Collection.Name.Id, AttributeId.From(27), ValueTags.Utf8),
            new DbAttribute(Collection.Loadout.Id, AttributeId.From(28), ValueTags.Reference),
            new DbAttribute(Collection.Mods.Id, AttributeId.From(29), ValueTags.Reference)
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
