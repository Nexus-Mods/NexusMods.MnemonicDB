using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class EntityContext(IEventStore store) : IEntityContext
{
    private TransactionId asOf = TransactionId.From(0);
    private object _lock = new object();

    private ConcurrentDictionary<EntityId, IEntity> _entities = new();
    private ConcurrentDictionary<EntityId, Dictionary<IAttribute, IAccumulator>> _values = new();


    public TEntity Get<TEntity>(EntityId<TEntity> id) where TEntity : IEntity
    {
        if (_entities.TryGetValue(id.Value, out var entity))
        {
            return (TEntity) entity;
        }

        var values = GetValues(id.Value);
        var type = (Type)values[IEntity.TypeAttribute].Get();

        var newEntity = (TEntity)Activator.CreateInstance(type, this, id)!;
        if (_entities.TryAdd(id.Value, newEntity))
        {
            return newEntity;
        }

        return (TEntity)_entities[id.Value];
    }

    private Dictionary<IAttribute, IAccumulator> GetValues(EntityId id)
    {
        if (_values.TryGetValue(id, out var values))
        {
            return values;
        }
        var newValues = LoadValues(id);
        return _values.TryAdd(id, newValues) ? newValues : _values[id];
    }


    private Dictionary<IAttribute, IAccumulator> LoadValues(EntityId id)
    {
        var values = new Dictionary<IAttribute, IAccumulator>();
        var ingester = new EntityContextIngester(values, id);
        store.EventsForEntity(id, ingester);
        return values;
    }

    public TEntity Get<TEntity>() where TEntity : ISingletonEntity
    {
        var id = TEntity.SingletonId;
        if (_entities.TryGetValue(id, out var entity))
        {
            return (TEntity) entity;
        }

        var newEntity = (TEntity)Activator.CreateInstance(typeof(TEntity), this, id)!;
        if (_entities.TryAdd(id, newEntity))
        {
            return newEntity;
        }

        return (TEntity)_entities[id];
    }

    public TransactionId Add<TEvent>(TEvent newEvent) where TEvent : IEvent
    {
        lock (_lock)
        {
            var newId = store.Add(newEvent);
            asOf = newId;

            var ingester = new ForwardEventContext(_values);
            newEvent.Apply(ingester);

            return newId;
        }
    }
    public IAccumulator GetAccumulator<TOwner, TAttribute>(EntityId ownerId, TAttribute attributeDefinition)
        where TOwner : IEntity where TAttribute : IAttribute
    {
        var values = GetValues(ownerId);
        return values[attributeDefinition];

    }
}
