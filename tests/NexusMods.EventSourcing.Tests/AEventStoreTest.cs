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
        var evt = CreateLoadout.Create("Test");
        Store.Add(evt);
        for (var i = 0; i < 10; i++)
        {
            Store.Add(new RenameLoadout(evt.Id, $"Test {i}"));
        }

        var accumulator = new EventAccumulator();
        Store.EventsForEntity(evt.Id.Value, accumulator);
        accumulator.Events.Count.Should().Be(11);
        accumulator.Events[0].Should().BeEquivalentTo(evt);
        for (var i = 1; i < 11; i++)
        {
            accumulator.Events[i].Should().BeEquivalentTo(new RenameLoadout(evt.Id, $"Test {i - 1}"));
        }
    }

    private class EventAccumulator : IEventIngester
    {
        public List<IEvent> Events { get; } = new();
        public void Ingest(IEvent @event)
        {
            Events.Add(@event);
        }
    }


}
