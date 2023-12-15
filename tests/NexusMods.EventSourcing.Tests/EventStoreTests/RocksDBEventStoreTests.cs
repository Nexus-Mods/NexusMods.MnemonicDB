using NexusMods.EventSourcing.RocksDB;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Tests.EventStoreTests;

public class RocksDBEventStoreTests(EventSerializer serializer) : AEventStoreTest<RocksDBEventStore<EventSerializer>>(
    new RocksDBEventStore<EventSerializer>(new Settings
    {
        StorageLocation = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("FasterKV.EventStore" + Guid.NewGuid())
    }, serializer));
