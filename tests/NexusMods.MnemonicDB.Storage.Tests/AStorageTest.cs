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

public abstract class AStorageTest : IDisposable
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
        Registry = new AttributeRegistry(provider.GetServices<IAttribute>());
        Registry.Populate([
            new DbAttribute(File.Path.Id, AttributeId.From(20), ValueTags.Utf8Insensitive, File.Path),
            new DbAttribute(File.Hash.Id, AttributeId.From(21), ValueTags.UInt64, File.Hash),
            new DbAttribute(File.Size.Id, AttributeId.From(22), ValueTags.UInt64, File.Size),
            new DbAttribute(File.ModId.Id, AttributeId.From(23), ValueTags.Reference, File.ModId),
            new DbAttribute(Mod.Name.Id, AttributeId.From(24), ValueTags.Utf8, Mod.Name),
            new DbAttribute(Mod.LoadoutId.Id, AttributeId.From(25), ValueTags.Reference, Mod.LoadoutId),
            new DbAttribute(Loadout.Name.Id, AttributeId.From(26), ValueTags.Utf8, Loadout.Name),
            new DbAttribute(Collection.Name.Id, AttributeId.From(27), ValueTags.Utf8, Collection.Name),
            new DbAttribute(Collection.LoadoutId.Id, AttributeId.From(28), ValueTags.Reference, Collection.LoadoutId),
            new DbAttribute(Collection.ModIds.Id, AttributeId.From(29), ValueTags.Reference, Collection.ModIds),
            new DbAttribute(Blobs.InKeyBlob.Id, AttributeId.From(30), ValueTags.Blob, Blobs.InKeyBlob),
            new DbAttribute(Blobs.InValueBlob.Id, AttributeId.From(31), ValueTags.HashedBlob, Blobs.InValueBlob),
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
    public void Dispose()
    {
        Registry.Dispose();
    }

    public EntityId NextTempId()
    {
        var partition = PartitionId.Entity;
        var id = Interlocked.Increment(ref _tempId);
        id |= (ulong)partition << 40;
        return PartitionId.Temp.MakeEntityId(id);
    }
}
