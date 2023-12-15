using NexusMods.EventSourcing.RocksDB;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Tests.EventStoreTests;

public class RocksDBEventStoreTests(EventSerializer serializer) : AEventStoreTest<RocksDBEventStore<EventSerializer>>(
    new RocksDBEventStore<EventSerializer>(serializer, new Settings
    {
        StorageLocation = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("FasterKV.EventStore" + Guid.NewGuid())
    }));
