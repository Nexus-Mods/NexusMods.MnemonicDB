using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel;

namespace NexusMods.EventSourcing.Tests.Contexts;

public class TestContext(ILogger<TestContext> logger, EventSerializer serializer) : IEntityContext
{
    private readonly InMemoryEventStore<EventSerializer> _store = new(serializer);
    private readonly Dictionary<EntityId, IEntity> _entities = new();
    private readonly Dictionary<EntityId, Dictionary<IAttribute, IAccumulator>> _values = new();

    private TransactionId _currentTransactionId = TransactionId.From(0);

    public TEntity Get<TEntity>(EntityId<TEntity> id) where TEntity : IEntity
    {
        if (_entities.TryGetValue(id.Value, out var entity))
        {
            return (TEntity)entity;
        }

        var values = LoadValues(id.Value);

        var type = (Type)values[IEntity.TypeAttribute].Get();
        var createdEntity = (TEntity)Activator.CreateInstance(type, this, id.Value)!;
        _entities.Add(id.Value, createdEntity);

        return createdEntity;
    }

    public TEntity Get<TEntity>() where TEntity : ISingletonEntity
    {
        var id = TEntity.SingletonId;
        if (_entities.TryGetValue(id, out var entity))
        {
            return (TEntity)entity;
        }

        var instance = (TEntity)Activator.CreateInstance(typeof(TEntity), this)!;
        _entities.Add(id, instance);
        return instance;
    }

    private Dictionary<IAttribute, IAccumulator> LoadValues(EntityId id)
    {
        var ingester = new Ingester(id);
        _store.EventsForEntity(id, ingester);
        _values.Add(id, ingester.Values);
        return ingester.Values;
    }

    public async ValueTask Add<TEvent>(TEvent entity) where TEvent : IEvent
    {
        _values.Clear();
        await _store.Add(entity);
    }

    public IAccumulator GetAccumulator<TOwner, TAttribute>(EntityId ownerId, TAttribute attributeDefinition)
        where TOwner: IEntity
        where TAttribute : IAttribute
    {
        if (_values.TryGetValue(ownerId, out var values))
            return values[attributeDefinition];

        var loadedValues = LoadValues(ownerId);
        return loadedValues[attributeDefinition];
    }

    private readonly struct Ingester(EntityId id) : IEventIngester, IEventContext
    {
        public readonly Dictionary<IAttribute,IAccumulator> Values  = new();

        public ValueTask Ingest(IEvent @event)
        {
            @event.Apply(this);
            return ValueTask.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IAccumulator GetAccumulator(IAttribute attribute)
        {
            if (!Values.TryGetValue(attribute, out var accumulator))
            {
                accumulator = attribute.CreateAccumulator();
                Values.Add(attribute, accumulator);
            }
            return accumulator;
        }

        public void Emit<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value)
            where TOwner : IEntity
        {
            if (entity.Value != id.Value)
                return;

            var accumulator = GetAccumulator(attr);
            accumulator.Add(value!);
        }

        public void Emit<TOwner, TVal>(EntityId entity, AttributeDefinition<TOwner, TVal> attr, TVal value)
            where TOwner : IEntity
        {
            if (entity.Value != id.Value)
                return;

            var accumulator = GetAccumulator(attr);
            accumulator.Add(value!);
        }

        public void Emit<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
        {
            if (entity.Value != id.Value)
                return;

            var accumulator = GetAccumulator(attr);
            accumulator.Add(value);
        }

        public void Retract<TOwner, TVal>(EntityId<TOwner> entity, AttributeDefinition<TOwner, TVal> attr, TVal value) where TOwner : IEntity
        {
            if (entity.Value != id.Value)
                return;

            var accumulator = GetAccumulator(attr);
            accumulator.Retract(value!);
        }

        public void Retract<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
        {
            if (entity.Value != id.Value)
                return;

            var accumulator = GetAccumulator(attr);
            accumulator.Retract(value);
        }

        public void New<TType>(EntityId<TType> newId) where TType : IEntity
        {
            if (newId.Value != id.Value)
                return;

            Emit(newId.Value, IEntity.TypeAttribute, typeof(TType));
        }
    }
}
