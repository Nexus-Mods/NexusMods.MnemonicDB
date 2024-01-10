using System.Buffers;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel;

public class InMemoryEventStore<TSerializer>(TSerializer serializer) : IEventStore
where TSerializer : IEventSerializer
{
    private TransactionId _tx = TransactionId.From(0);
    private readonly Dictionary<EntityId,IList<(TransactionId TxId, byte[] Data)>> _events = new();

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
                    value = new List<(TransactionId, byte[])>();
                    _events.Add(id, value);
                }

                value.Add((_tx, data.ToArray()));
            }

            return _tx;
        }
    }


    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester)
        where TIngester : IEventIngester
    {
        if (!_events.TryGetValue(entityId, out var events))
            return;
        foreach (var data in events)
        {
            var @event = serializer.Deserialize(data.Data)!;
            if (!ingester.Ingest(data.TxId, @event)) break;
        }
    }
}
