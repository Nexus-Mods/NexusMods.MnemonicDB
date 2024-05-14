using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.Storage.Tests.TestAttributes;
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
            new DbAttribute(File.Attributes.Path.Id, AttributeId.From(20), ValueTags.Utf8Insensitive),
            new DbAttribute(File.Attributes.Hash.Id, AttributeId.From(21), ValueTags.UInt64),
            new DbAttribute(File.Attributes.Size.Id, AttributeId.From(22), ValueTags.UInt64),
            new DbAttribute(File.Attributes.ModId.Id, AttributeId.From(23), ValueTags.Reference),
            new DbAttribute(Mod.Attributes.Name.Id, AttributeId.From(24), ValueTags.Utf8),
            new DbAttribute(Mod.Attributes.LoadoutId.Id, AttributeId.From(25), ValueTags.Reference),
            new DbAttribute(Loadout.Attributes.Name.Id, AttributeId.From(26), ValueTags.Utf8),
            new DbAttribute(Collection.Attributes.Name.Id, AttributeId.From(27), ValueTags.Utf8),
            new DbAttribute(Collection.Attributes.LoadoutId.Id, AttributeId.From(28), ValueTags.Reference),
            new DbAttribute(Collection.Attributes.ModIds.Id, AttributeId.From(29), ValueTags.Reference),
            new DbAttribute(Blobs.InKeyBlob.Id, AttributeId.From(30), ValueTags.Blob),
            new DbAttribute(Blobs.InValueBlob.Id, AttributeId.From(31), ValueTags.HashedBlob)
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
        await ((DatomStore)DatomStore).StartAsync(CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        Registry.Dispose();
        return Task.CompletedTask;
    }

    public EntityId NextTempId(byte partition = (byte)Ids.Partition.Entity)
    {
        var id = Interlocked.Increment(ref _tempId);
        id |= (ulong)partition << 40;
        return EntityId.From(Ids.MakeId(Ids.Partition.Tmp, id));
    }
}
