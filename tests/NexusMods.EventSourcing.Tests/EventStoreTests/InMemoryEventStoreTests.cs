using NexusMods.EventSourcing.TestModel;

namespace NexusMods.EventSourcing.Tests.EventStoreTests;

public class InMemoryEventStoreTests : AEventStoreTest<InMemoryEventStore<EventSerializer>>
{
    public InMemoryEventStoreTests(EventSerializer serializer) : base(new InMemoryEventStore<EventSerializer>(serializer)) { }
}
