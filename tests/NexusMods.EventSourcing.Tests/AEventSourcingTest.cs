using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage;
using NexusMods.EventSourcing.Storage.InMemoryBackend;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Tests;

public class AEventSourcingTest : IAsyncLifetime
{
    protected Connection Connection = null!;
    protected DatomStoreSettings Config { get; set; }
    protected ILogger Logger;

    private DatomStore _store;
    private readonly IValueSerializer[] _valueSerializers;
    private readonly IAttribute[] _attributes;
    private readonly IServiceProvider _provider;
    private readonly AttributeRegistry _registry;


    protected AEventSourcingTest(IServiceProvider provider)
    {
        _provider = provider;
        _valueSerializers = provider.GetRequiredService<IEnumerable<IValueSerializer>>().ToArray();
        _attributes = provider.GetRequiredService<IEnumerable<IAttribute>>().ToArray();

        _registry = new AttributeRegistry(_valueSerializers, _attributes);

        Config = new DatomStoreSettings
        {
            Path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
                .Combine("tests_eventsourcing" + Guid.NewGuid())
        };

        _store = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), _registry, Config, new Backend(_registry));

        Logger = provider.GetRequiredService<ILogger<AEventSourcingTest>>();

    }


    protected async Task RestartDatomStore()
    {

        _store.Dispose();


        _store = new DatomStore(_provider.GetRequiredService<ILogger<DatomStore>>(), _registry, Config, new Backend(_registry));
        await _store.Sync();

        Connection = await Connection.Start(_store, _valueSerializers, _attributes);
    }

    public async Task InitializeAsync()
    {
        await _store.Sync();

        Connection = await Connection.Start(_store, _valueSerializers, _attributes);
    }

    public Task DisposeAsync()
    {
        _store.Dispose();
        return Task.CompletedTask;
    }
}
