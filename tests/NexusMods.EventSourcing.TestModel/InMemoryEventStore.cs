using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel;

public class InMemoryEventStore<TSerializer>(TSerializer serializer) : IEventStore
where TSerializer : IEventSerializer
{
    private TransactionId _tx = TransactionId.From(0);
    private readonly Dictionary<EntityId,IList<byte[]>> _events = new();

    public TransactionId Add<T>(T entity) where T : IEvent
    {
        lock (this)
        {
            _tx = _tx.Next();
            var data = serializer.Serialize(entity);
            var logger = new ModifiedEntitiesIngester();
            entity.Apply(logger);
            foreach (var id in logger.Entities)
            {
                if (!_events.TryGetValue(id, out var value))
                {
                    value = new List<byte[]>();
                    _events.Add(id, value);
                }

                value.Add(data.ToArray());
            }

            return _tx;
        }
    }


    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester)
        where TIngester : IEventIngester
    {
        foreach (var data in _events[entityId])
        {
            var @event = serializer.Deserialize(data)!;
            ingester.Ingest(@event);
        }
    }
}
