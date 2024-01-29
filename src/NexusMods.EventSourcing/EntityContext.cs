using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DynamicData;
using Microsoft.Extensions.ObjectPool;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.AttributeDefinitions;

namespace NexusMods.EventSourcing;

/// <summary>
/// The primary way of interacting with the events. This class is thread safe, and can be used to lookup entities
/// and insert new events.
/// </summary>
public class EntityContext : IEntityContext
{
    /// <summary>
    /// The maximum number of events that can be processed before a snapshot is taken.
    /// </summary>
    private const int MaxEventsBeforeSnapshotting = 250;

    private TransactionId _asOf;
    private readonly object _lock = new();

    private readonly IndexerIngester _indexerIngester = new();
    private readonly List<(IIndexableAttribute, IAccumulator)> _indexUpdaters = new();
    private readonly HashSet<(EntityId, string)> _updatedAttributes = new();

    private readonly ConcurrentDictionary<EntityId, IEntity> _entities = new();
    private readonly ConcurrentDictionary<EntityId, Dictionary<IAttribute, IAccumulator>> _values = new();

    private readonly ObjectPool<EntityIdDefinitionAccumulator> _definitionAccumulatorPool =
        new DefaultObjectPool<EntityIdDefinitionAccumulator>(new DefaultPooledObjectPolicy<EntityIdDefinitionAccumulator>());

    private static readonly MethodInfo ConstructMethod = typeof(EntityContext).GetMethod(nameof(Construct), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly IEventStore _store;

    /// <summary>
    /// The primary way of interacting with the events. This class is thread safe, and can be used to lookup entities
    /// and insert new events.
    /// </summary>
    /// <param name="store"></param>
    public EntityContext(IEventStore store)
    {
        _store = store;
        _asOf = store.TxId;
    }

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
        if (_entities.TryGetValue(id.Id, out var entity))
            return (TEntity) entity;

        var type = IEntity.TypeAttribute.Get(this, id.Id);

        Debug.Assert(type.IsAssignableTo(typeof(TEntity)), $"type.IsSubclassOf(typeof(TEntity)) for {type} and {typeof(TEntity)}");

        TEntity newEntity;
        if (EntityStructureRegistry.TryGetSingleton(id.Id, out var definition))
        {
            if (definition.Type != type)
                throw new InvalidOperationException("Singleton Entity type mismatch");

            newEntity = (TEntity)Activator.CreateInstance(type, this)!;
        }
        else
        {
            Debug.Assert(type.IsAssignableTo(typeof(TEntity)), $"type.IsSubclassOf(typeof(TEntity)) for {type} and {typeof(TEntity)}");
            // TODO: Cache this and/or use linq expression
            newEntity = (TEntity)ConstructMethod.MakeGenericMethod(typeof(TEntity), type).Invoke(this, [id.Id])!;
        }

        if (_entities.TryAdd(id.Id, newEntity))
            return newEntity;

        return (TEntity)_entities[id.Id];
    }

    private TAbstract Construct<TAbstract, TConcrete>(EntityId id) where TConcrete : TAbstract, IEntity
    {
        var casted = EntityId<TConcrete>.From(id);
        var newEntity = (TAbstract)Activator.CreateInstance(typeof(TConcrete), this, casted)!;
        return newEntity;
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

        var snapshotTxId = _store.GetSnapshot(_asOf, id, out var loadedDefinition, out var loadedAttributes);

        if (snapshotTxId != TransactionId.Min)
        {
            values.Add(IEntity.TypeAttribute, loadedDefinition);
            foreach (var (attr, accumulator) in loadedAttributes)
                values.Add(attr, accumulator);
        }

        var ingester = new EntityContextIngester(values, id);
        _store.EventsForIndex(IEntity.EntityIdAttribute, id, ingester, snapshotTxId, _asOf);

        if (ingester.ProcessedEvents > MaxEventsBeforeSnapshotting)
        {
            var snapshot = new Dictionary<IAttribute, IAccumulator>();

            if (EntityStructureRegistry.TryGetSingleton(id, out var definition))
            {
                var accumulator = IEntity.TypeAttribute.CreateAccumulator();
                accumulator.Value = definition;
                snapshot.Add(IEntity.TypeAttribute, accumulator);
            }

            foreach (var (attr, accumulator) in values)
                snapshot.Add(attr, accumulator);

            _store.SetSnapshot(ingester.LastTransactionId, id, snapshot);
        }

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
        // Look in the cache first
        var id = TEntity.SingletonId;
        if (_entities.TryGetValue(id, out var entity))
            return (TEntity) entity;

        var newEntity = (TEntity)Activator.CreateInstance(typeof(TEntity), this)!;

        Debug.Assert(id != default, nameof(id) + " != default");

        if (_entities.TryAdd(id, newEntity))
            return newEntity;

        return (TEntity)_entities[id];
    }

    /// <summary>
    /// Adds a new event to the store, and updates the cache. This method is thread safe as the system is considered
    /// single-writer, multiple-reader.
    /// </summary>
    /// <param name="newEvent"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    public TransactionId Add<TEvent>(TEvent newEvent) where TEvent : IEvent
    {
        lock (_lock)
        {

            // Reset the indexer ingester and ingest the new event
            _indexerIngester.Reset();
            _indexerIngester.Ingest(TransactionId.Min, newEvent);

            // Reset the index updaters
            _indexUpdaters.Clear();

            // Add the entity id to the index updaters, use the object pool to reduce allocations
            foreach (var entityId in _indexerIngester.Ids)
            {
                var accumulator = _definitionAccumulatorPool.Get();
                accumulator.Id = entityId;
                _indexUpdaters.Add((IEntity.EntityIdAttribute, accumulator));
            }

            // These are less common, so we don't optimize them quite so much
            foreach (var (attribute, accumulators) in _indexerIngester.IndexedAttributes)
            {
                foreach (var accumulator in accumulators)
                {
                    _indexUpdaters.Add((attribute, accumulator));
                }
            }

            // Add the event to the store
            var newId = _store.Add(newEvent, _indexUpdaters);

            // Return the definition accumulators to the pool
            for (var idx = 0; idx < _indexerIngester.Ids.Count; idx++)
            {
                var (_, accumulator) = _indexUpdaters[idx];
                _definitionAccumulatorPool.Return((EntityIdDefinitionAccumulator)accumulator);
            }

            // Update the asOf transaction id
            _asOf = newId;

            // Clear the updated attributes
            _updatedAttributes.Clear();

            // Ingest the event again to update the cache and notify any listeners, ForwardEventContext
            // is a struct so it will be stack allocated
            var ingester = new ForwardEventContext(_values, _updatedAttributes);
            newEvent.Apply(ingester);

            // Notify any listeners
            foreach (var (entityId, attributeName) in _updatedAttributes)
            {
                if (_entities.TryGetValue(entityId, out var entity))
                    entity.OnPropertyChanged(attributeName);
            }

            return newId;
        }
    }

    /// <inheritdoc />
    public bool GetReadOnlyAccumulator<TOwner, TAttribute, TAccumulator>(EntityId ownerId, TAttribute attributeDefinition,
        out TAccumulator accumulator, bool createIfMissing = false)
        where TOwner : IEntity
        where TAttribute : IAttribute<TAccumulator>
        where TAccumulator : IAccumulator
    {
        var values = GetAccumulators(ownerId);
        if (values.TryGetValue(attributeDefinition, out var value))
        {
            accumulator = (TAccumulator) value;
            return true;
        }

        if (createIfMissing)
        {
            var newAccumulator = attributeDefinition.CreateAccumulator();
            if (values.TryAdd(attributeDefinition, newAccumulator))
            {
                accumulator = newAccumulator;
                return true;
            }
            accumulator = (TAccumulator) values[attributeDefinition];
            return true;
        }

        accumulator = default!;
        return false;
    }

    /// <inheritdoc />
    public ITransaction Begin()
    {
        return new Transaction(this, new List<IEvent>());
    }

    /// <summary>
    /// Empties all caches, any existing entities will be stale and likely no longer work, use only for testing.
    /// </summary>
    public void EmptyCaches()
    {
        _entities.Clear();
        _values.Clear();
        _asOf = _store.TxId;
    }

    /// <summary>
    /// Gets all the entities that have the given attribute set to the given value.
    /// </summary>
    /// <param name="attr"></param>
    /// <param name="val"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    /// <returns></returns>
    public IEnumerable<TEntity> EntitiesForIndex<TEntity, TVal>(IIndexableAttribute<TVal> attr, TVal val)
        where TEntity : IEntity
    {
        var foundEntities = new HashSet<EntityId<TEntity>>();
        _store.EventsForIndex(attr, val, new SecondaryIndexIngester<TEntity>(foundEntities), TransactionId.Min, _asOf);

        foreach (var entityId in foundEntities)
        {
            var entity =  Get(entityId);

            if (!_values.TryGetValue(entityId.Id, out var values)) continue;

            if (!values.TryGetValue(attr, out var accumulator)) continue;

            if (attr.Equal(accumulator, val))
            {
                yield return entity;
            }

        }
    }

    private readonly struct SecondaryIndexIngester<TType>(HashSet<EntityId<TType>> foundEntities) : IEventIngester,
        IEventContext where TType : IEntity
    {

        public bool Ingest(TransactionId id, IEvent @event)
        {
            @event.Apply(this);
            return true;
        }

        public bool GetAccumulator<TOwner, TAttribute, TAccumulator>(EntityId<TOwner> entityId, TAttribute attributeDefinition,
            out TAccumulator accumulator) where TOwner : IEntity where TAttribute : IAttribute<TAccumulator> where TAccumulator : IAccumulator
        {
            if (typeof(TOwner) == typeof(TType))
            {
                foundEntities.Add(entityId.Cast<TType>());
            }
            accumulator = default!;
            return false;
        }
    }
}
