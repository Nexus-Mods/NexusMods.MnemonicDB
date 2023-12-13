using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel;

public class InMemoryEventStore<TSerializer>(TSerializer serializer) : IEventStore
where TSerializer : IEventSerializer
{
    private readonly Dictionary<EntityId,IList<byte[]>> _events = new();

    public async ValueTask Add<T>(T entity) where T : IEvent
    {
        var data = serializer.Serialize(entity);
        var logger = new ModifiedEntityLogger();
        entity.Apply(logger);
        lock (_events)
        {
            foreach (var id in logger.Entities)
            {
                if (!_events.TryGetValue(id, out var value))
                {
                    value = new List<byte[]>();
                    _events.Add(id, value);
                }
                value.Add(data);
            }
        }
    }

    /// <summary>
    /// Simplistic context that just logs the entities that were modified.
    /// </summary>
    private readonly struct ModifiedEntityLogger() : IEventContext
    {
        public readonly HashSet<EntityId> Entities  = new();
        public void Emit<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value) where TOwner : IEntity
        {
            Entities.Add(entity.Value);
        }

        public void Emit<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
        {
            Entities.Add(entity.Value);
        }

        public void Retract<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value) where TOwner : IEntity
        {
            Entities.Add(entity.Value);
        }

        public void Retract<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
        {
            Entities.Add(entity.Value);
        }

        public void New<TType>(EntityId<TType> id) where TType : IEntity
        {
            Entities.Add(id.Value);
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
