using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage;

namespace NexusMods.EventSourcing.Tests;

public class AEventSourcingTest : IAsyncLifetime
{
    protected Connection Connection = null!;
    private readonly DatomStore _store;
    private readonly NodeStore _nodeStore;
    private readonly IValueSerializer[] _valueSerializers;
    private readonly IAttribute[] _attributes;
    private readonly InMemoryKvStore _kvStore;

    protected AEventSourcingTest(IServiceProvider provider)
    {
        _valueSerializers = provider.GetRequiredService<IEnumerable<IValueSerializer>>().ToArray();
        _attributes = provider.GetRequiredService<IEnumerable<IAttribute>>().ToArray();

        var registry = new AttributeRegistry(_valueSerializers, _attributes);
        _kvStore = new InMemoryKvStore();
        _nodeStore = new NodeStore(provider.GetRequiredService<ILogger<NodeStore>>(), _kvStore, registry);

        _store = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), _nodeStore, registry);

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
