using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel;

namespace NexusMods.EventSourcing.Tests.EventStoreTests;

public class InMemoryEventStoreTests : AEventStoreTest<InMemoryEventStore<BinaryEventSerializer>>
{
    public InMemoryEventStoreTests(BinaryEventSerializer serializer) : base(new InMemoryEventStore<BinaryEventSerializer>(serializer)) { }
}
