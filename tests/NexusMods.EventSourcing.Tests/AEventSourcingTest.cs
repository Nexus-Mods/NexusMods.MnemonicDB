using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage;

namespace NexusMods.EventSourcing.Tests;

public class AEventSourcingTest
{
    protected readonly Connection Connection;

    protected AEventSourcingTest(IServiceProvider provider)
    {
        var valueSerializers = provider.GetRequiredService<IEnumerable<IValueSerializer>>().ToArray();
        var attributes = provider.GetRequiredService<IEnumerable<IAttribute>>().ToArray();

        var registry = new AttributeRegistry(valueSerializers, attributes);
        var kvStore = new InMemoryKvStore();
        var nodeStore = new NodeStore(provider.GetRequiredService<ILogger<NodeStore>>(), kvStore, registry);

        var store = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), nodeStore, registry);
        Connection = new Connection(store, attributes, valueSerializers);
    }
}
