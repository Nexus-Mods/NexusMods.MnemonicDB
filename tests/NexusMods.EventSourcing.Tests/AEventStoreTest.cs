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
    }

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
