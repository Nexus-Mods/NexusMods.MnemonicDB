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
        Store.Add(new CreateLoadout(enityId, "Test"));
        for (var i = 0; i < 10; i++)
        {
            Store.Add(new RenameLoadout(enityId, $"Test {i}"));
        }

        var accumulator = new EventAccumulator();
        Store.EventsForEntity(enityId.Value, accumulator);
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

        snapshotId.Should().Be(TransactionId.From(1024));

    }

    private class EventAccumulator : IEventIngester
    {
        public List<IEvent> Events { get; } = new();
        public bool Ingest(TransactionId _, IEvent @event)
        {
            Events.Add(@event);
            return true;
        }
    }


}
