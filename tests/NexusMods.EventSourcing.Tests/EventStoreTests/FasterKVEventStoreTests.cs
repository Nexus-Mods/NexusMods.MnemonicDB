using NexusMods.EventSourcing.FasterKV;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Tests.EventStoreTests;

public class FasterKVEventStoreTests : AEventStoreTest<FasterKVEventStore<EventSerializer>>
{
    public FasterKVEventStoreTests(EventSerializer serializer) : base(new FasterKVEventStore<EventSerializer>(serializer, new Settings()
    {
        StorageLocation = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("FasterKV.EventStore" + Guid.NewGuid())
    })) { }
}
