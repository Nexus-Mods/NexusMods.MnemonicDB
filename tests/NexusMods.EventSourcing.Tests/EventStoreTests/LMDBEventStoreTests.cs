using NexusMods.EventSourcing.LMDB;
using NexusMods.Paths;
using Settings = NexusMods.EventSourcing.RocksDB.Settings;

namespace NexusMods.EventSourcing.Tests.EventStoreTests;

public class LMDBEventStoreTests(EventSerializer serializer) : AEventStoreTest<LMDBEventStore<EventSerializer>>(
    new LMDBEventStore<EventSerializer>(serializer, new LMDB.Settings
    {
        StorageLocation = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("FasterKV.EventStore" + Guid.NewGuid())
    }));
