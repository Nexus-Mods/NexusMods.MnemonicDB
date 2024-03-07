using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage;

namespace NexusMods.EventSourcing.Tests;

public class AEventSourcingTest : IAsyncLifetime
{
    protected Connection Connection = null!;
    protected DatomStoreSettings Config { get; set; }
    protected ILogger Logger;

    private DatomStore _store;
    private readonly NodeStore _nodeStore;
    private readonly IValueSerializer[] _valueSerializers;
    private readonly IAttribute[] _attributes;
    private readonly InMemoryKvStore _kvStore;
    private readonly IServiceProvider _provider;
    private readonly AttributeRegistry _registry;


    protected AEventSourcingTest(IServiceProvider provider)
    {
        _provider = provider;
        _valueSerializers = provider.GetRequiredService<IEnumerable<IValueSerializer>>().ToArray();
        _attributes = provider.GetRequiredService<IEnumerable<IAttribute>>().ToArray();

        _registry = new AttributeRegistry(_valueSerializers, _attributes);
        _kvStore = new InMemoryKvStore();
        _nodeStore = new NodeStore(provider.GetRequiredService<ILogger<NodeStore>>(), _kvStore, _registry);

        Config = new DatomStoreSettings();
        _store = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), _nodeStore, _registry, Config);

        Logger = provider.GetRequiredService<ILogger<AEventSourcingTest>>();

    }


    protected async Task RestartDatomStore()
    {

        _store.Dispose();


        _store = new DatomStore(_provider.GetRequiredService<ILogger<DatomStore>>(), _nodeStore, _registry, Config);
        await _store.Sync();

        Connection = await Connection.Start(_store, _valueSerializers, _attributes);
    }

    public async Task InitializeAsync()
    {
        await _store.Sync();

        Connection = await Connection.Start(_store, _valueSerializers, _attributes);
    }

    public async Task DisposeAsync()
    {
        _store.Dispose();
    }
}
