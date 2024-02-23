using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Tests;

public class AEventSourcingTest
{
    protected readonly Connection Connection;

    protected AEventSourcingTest(IEnumerable<IValueSerializer> valueSerializers,
        IEnumerable<IAttribute> attributes)
    {
        var valueSerializerArray = valueSerializers.ToArray();

        var attributeArray = attributes.ToArray();
        var registry = new AttributeRegistry(valueSerializerArray, attributeArray);
        var kvStore = new InMemoryKvStore();
        var store = new DatomStore(kvStore, registry);
        Connection = new Connection(store, attributeArray, valueSerializerArray);
    }
}
