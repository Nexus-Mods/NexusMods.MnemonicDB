using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Serializers;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Storage.Tests;

public abstract class AStorageTest : IAsyncLifetime
{
    protected readonly AttributeRegistry _registry;
    protected IDatomStore DatomStore;
    protected readonly DatomStoreSettings DatomStoreSettings;

    protected readonly ILogger Logger;
    private readonly IServiceProvider _provider;
    private readonly AbsolutePath _path;

    private ulong _tempId = 1;

    protected AStorageTest(IServiceProvider provider)
    {
        _provider = provider;
        _registry = new AttributeRegistry(provider.GetRequiredService<IEnumerable<IValueSerializer>>(),
            provider.GetRequiredService<IEnumerable<IAttribute>>());
        _registry.Populate([
            new DbAttribute(Symbol.Intern<ModAttributes.Name>(), AttributeId.From(10), Symbol.Intern<StringSerializer>())
        ]);
        _path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("tests_datomstore"+Guid.NewGuid());

        DatomStoreSettings = new()
        {
            Path = _path,
        };


        DatomStore = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), _registry, DatomStoreSettings);
        Logger = provider.GetRequiredService<ILogger<AStorageTest>>();
    }

    public EntityId NextTempId()
    {
        var id = Interlocked.Increment(ref _tempId);
        return EntityId.From(Ids.MakeId(Ids.Partition.Tmp, id));
    }

    public async Task InitializeAsync()
    {
        await DatomStore.Sync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
