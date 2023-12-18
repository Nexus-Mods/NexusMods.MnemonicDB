using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class EntityContext(IEventStore store) : IEntityContext
{
    private TransactionId asOf = TransactionId.From(0);
    private object _lock = new object();

    private ConcurrentDictionary<EntityId, IEntity> _entities = new();
    private ConcurrentDictionary<EntityId, Dictionary<IAttribute, IAccumulator>> _values = new();


    /// <summary>
    /// Gets an entity by its id. The resulting entity type will be the type that was emitted by the event that created
    /// the entity (via the <see cref="IEntity.TypeAttribute"/> attribute). The resulting entity will be cast to the
    /// type specified by the generic parameter.
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public TEntity Get<TEntity>(EntityId<TEntity> id) where TEntity : IEntity
    {
        if (_entities.TryGetValue(id.Value, out var entity))
            return (TEntity) entity;

        var type = IEntity.TypeAttribute.Get(this, id.Value);
        var newEntity = (TEntity)Activator.CreateInstance(type, this, id)!;

        if (_entities.TryAdd(id.Value, newEntity))
            return newEntity;

        return (TEntity)_entities[id.Value];
    }

    /// <summary>
    /// Gets all the accumulators for the given entity id. Will use the cache if available, otherwise will replay all
    /// events for the entity id, via the LoadAccumulators method.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private Dictionary<IAttribute, IAccumulator> GetAccumulators(EntityId id)
    {
        if (_values.TryGetValue(id, out var values))
            return values;

        var newValues = LoadAccumulators(id);
        return _values.TryAdd(id, newValues) ? newValues : _values[id];
    }

    /// <summary>
    /// Replays all the events for the given entity id and returns the resulting accumulators.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private Dictionary<IAttribute, IAccumulator> LoadAccumulators(EntityId id)
    {
        var values = new Dictionary<IAttribute, IAccumulator>();
        var ingester = new EntityContextIngester(values, id);
        store.EventsForEntity(id, ingester);
        return values;
    }

    /// <summary>
    /// Gets the singleton entity of the specified type. No entity id is required as there is only ever one singleton
    /// and the entity Id is specified by the <see cref="ISingletonEntity.SingletonId"/> property.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public TEntity Get<TEntity>() where TEntity : ISingletonEntity
    {
        var id = TEntity.SingletonId;
        if (_entities.TryGetValue(id, out var entity))
            return (TEntity) entity;

        var newEntity = (TEntity)Activator.CreateInstance(typeof(TEntity), this)!;
        if (_entities.TryAdd(id, newEntity))
            return newEntity;

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

    /// <inheritdoc />
    public bool GetReadOnlyAccumulator<TOwner, TAttribute, TAccumulator>(EntityId<TOwner> ownerId, TAttribute attributeDefinition,
        out TAccumulator accumulator)
        where TOwner : IEntity
        where TAttribute : IAttribute<TAccumulator>
        where TAccumulator : IAccumulator
    {
        var values = GetAccumulators(ownerId.Value);
        if (values.TryGetValue(attributeDefinition, out var value))
        {
            accumulator = (TAccumulator) value;
            return true;
        }
        accumulator = default!;
        return false;
    }

    /// <summary>
    /// Empties all caches, any existing entities will be stale and likely no longer work, use only for testing.
    /// </summary>
    public void EmptyCaches()
    {
        _entities.Clear();
        _values.Clear();
    }
}
