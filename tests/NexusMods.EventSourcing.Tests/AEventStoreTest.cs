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

    [Fact]
    public void CanIterateInReverse()
    {
        var entityId = EntityId<Loadout>.NewId();
        var txIds = new List<(TransactionId Id, string Name)>();

        for (var i = 0; i < 1000; i++)
        {
            var name = $"Test {i}";
            var txid = Store.Add(new RenameLoadout(entityId, name));
            txIds.Add((txid, name));
        }

        var accumulator = new InverseChecker(txIds.ToArray());
        Store.EventsForEntity(entityId.Value, accumulator, true);

        accumulator.Count.Should().Be(txIds.Count);
    }

    private class InverseChecker((TransactionId Id, string Name)[] txEs, int count = 0) : IEventIngester
    {
        public int Count => count;

        public bool Ingest(TransactionId id, IEvent @event)
        {
            var current = txEs[^(count + 1)];
            count += 1;
            id.Should().Be(current.Id);
            ((RenameLoadout) @event).Name.Should().Be(current.Name);
            return true;
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
