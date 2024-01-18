using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Serialization;
using NexusMods.EventSourcing.RocksDB;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Tests.EventStoreTests;

public class RocksDBEventStoreTests(BinaryEventSerializer serializer, ISerializationRegistry serializationRegistry)
    : AEventStoreTest<RocksDBEventStore<BinaryEventSerializer>>(
        new RocksDBEventStore<BinaryEventSerializer>(serializer, new Settings
        {
            StorageLocation = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
                .Combine("FasterKV.EventStore" + Guid.NewGuid())
        }, serializationRegistry))

{


    [Fact]
    public void ReopeningAStoreShouldRetainTheSameTxId()
    {
        var name = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("FasterKV.EventStore_reopen" + Guid.NewGuid());

        TransactionId txId;
        {
            using var store = new RocksDBEventStore<BinaryEventSerializer>(serializer, new Settings
            {
                StorageLocation = name
            }, serializationRegistry);

            var entityContext = new EntityContext(store);

            using var tx = entityContext.Begin();
            CreateLoadout.Create(tx, "Loadout 1");
            CreateLoadout.Create(tx, "Loadout 2");
            txId = tx.Commit();
        }

        {
            using var store = new RocksDBEventStore<BinaryEventSerializer>(serializer, new Settings
            {
                StorageLocation = name
            }, serializationRegistry);

            var entityContext = new EntityContext(store);

            var registry = entityContext.Get<LoadoutRegistry>();
            registry.Loadouts.Count.Should().Be(2);

        }

    }

}
