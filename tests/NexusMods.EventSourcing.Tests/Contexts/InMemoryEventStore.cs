using MemoryPack;
using MemoryPack.Formatters;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Events;

namespace NexusMods.EventSourcing.Tests.Contexts;

public class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<EntityId,IList<byte[]>> _events = new();

    public InMemoryEventStore()
    {
        var formatter = new DynamicUnionFormatter<IEvent>(new[]
        {
            ( (ushort)3, typeof(CreateLoadout)),
            ( (ushort)4, typeof(AddMod)),
            ( (ushort)5, typeof(SwapModEnabled))
        });
        MemoryPackFormatterProvider.Register(formatter);
    }

    public ValueTask Add<T>(T entity) where T : IEvent
    {
        lock (_events)
        {
            var data = MemoryPackSerializer.Serialize(entity);
            entity.ModifiedEntities(id =>
            {
                if (!_events.TryGetValue(id, out var value))
                {
                    value = new List<byte[]>();
                    _events.Add(id, value);
                }
                value.Add(data);
            });
        }
        return ValueTask.CompletedTask;
    }


    public ValueTask EventsForEntity<TEntity, TIngester>(EntityId<TEntity> entityId, TIngester ingester)
        where TEntity : IEntity where TIngester : IEventIngester
    {
        foreach (var data in _events[entityId.Value])
        {
            var @event = MemoryPackSerializer.Deserialize<IEvent>(data)!;
            ingester.Ingest(@event);
        }
        return ValueTask.CompletedTask;
    }
}
