using NexusMods.EventSourcing.Abstractions.Serialization;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel;

namespace NexusMods.EventSourcing.Tests.EventStoreTests;

public class InMemoryEventStoreTests : AEventStoreTest<InMemoryEventStore<BinaryEventSerializer>>
{
    public InMemoryEventStoreTests(BinaryEventSerializer serializer, ISerializationRegistry serializationRegistry) :
        base(new InMemoryEventStore<BinaryEventSerializer>(serializer, serializationRegistry)) { }
}
