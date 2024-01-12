using NexusMods.EventSourcing.Abstractions.Serialization;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.Serialization;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Tests.EventStoreTests;

public class RocksDBEventStoreTests(BinaryEventSerializer serializer, ISerializationRegistry serializationRegistry) : AEventStoreTest<RocksDBEventStore<BinaryEventSerializer>>(
    new RocksDBEventStore<BinaryEventSerializer>(serializer, new Settings
    {
        StorageLocation = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("FasterKV.EventStore" + Guid.NewGuid())
    }, serializationRegistry));
