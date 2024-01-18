using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Tests;

public abstract class AEventStoreTest<T> where T : IEventStore
{
    protected T Store;

    public AEventStoreTest(T store)
    {
        Store = store;
        Context = new EntityContext(store);
    }

    public EntityContext Context { get; set; }

    [Fact]
    public void CanGetAndReturnEvents()
    {
        var enityId = EntityId<Loadout>.NewId();
        var entityIdAccumulator = IEntity.EntityIdAttribute.CreateAccumulator();
        entityIdAccumulator.Id = enityId.Value;

        var indexArray = new (IIndexableAttribute, IAccumulator)[] { (IEntity.EntityIdAttribute, entityIdAccumulator) };

        Store.Add(new CreateLoadout(enityId, "Test"), indexArray);

        for (var i = 0; i < 10; i++)
        {
            Store.Add(new RenameLoadout(enityId, $"Test {i}"), indexArray);
        }

        var accumulator = new EventIngester();
        Store.EventsForIndex(IEntity.EntityIdAttribute, enityId.Value, accumulator);
        accumulator.Events.Count.Should().Be(11);
        accumulator.Events[0].Should().BeEquivalentTo(new CreateLoadout(enityId, "Test"));
        for (var i = 1; i < 11; i++)
        {
            accumulator.Events[i].Should().BeEquivalentTo(new RenameLoadout(enityId, $"Test {i - 1}"));
        }
    }

    [Fact]
    public void CanGetSnapshots()
    {
        var id = EntityId<Loadout>.NewId();

        Context.Add(new CreateLoadout(id, "Test"));

        for (var i = 0; i < 1024; i++)
        {
            Context.Add(new RenameLoadout(id, $"Test {i}"));
        }

        Context.EmptyCaches();
        var loadout = Context.Get(id);

        loadout.Should().NotBeNull();
        loadout.Name.Should().Be("Test 1023");


        var snapshotId = Store.GetSnapshot(TransactionId.Max, id.Value, out var definition, out var attributes);

        snapshotId.Should().Be(TransactionId.From(1025));

        Context.EmptyCaches();

        // Load it again, this time from the cache
        loadout = Context.Get(id);
        loadout.Should().NotBeNull();
        loadout.Name.Should().Be("Test 1023");

    }

    private class EventIngester : IEventIngester
    {
        public List<IEvent> Events { get; } = new();
        public bool Ingest(TransactionId _, IEvent @event)
        {
            Events.Add(@event);
            return true;
        }
    }



    [Fact]
    public void CanQuerySecondaryIndexes()
    {

        var ctx = new EntityContext(Store);

        using (var tx = ctx.Begin())
        {
            AddArchive.Create(tx, 0xDEADBEEF, 1024, ("/foo/bar", 0x42, 0x43), ("/foo/baz", 0x44, 0x45));
            tx.Commit();
        }

        using (var tx = ctx.Begin())
        {
            AddArchive.Create(tx, 0xDEAD0000, 1024, ("/foo/buz", 0x42, 0x43));
            tx.Commit();
        }

        var entities = ctx.EntitiesForIndex<ArchiveEntry, ulong>(ArchiveEntry._hash, 0x42).ToArray();
        entities.Length.Should().Be(2);
        var names = entities.Select(e => e.Path).ToArray();
        names.Should().Contain("/foo/bar");
        names.Should().Contain("/foo/buz");





    }


}
