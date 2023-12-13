using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Tests.Contexts;

public class TestContext(ILogger<TestContext> logger, EventSerializer serializer) : IEntityContext
{
    private readonly InMemoryEventStore _store = new(serializer);
    private readonly Dictionary<EntityId, IEntity> _entities = new();
    private readonly Dictionary<EntityId, Dictionary<IAttribute, IAccumulator>> _values = new();

    private TransactionId _currentTransactionId = TransactionId.From(0);

    public TEntity Get<TEntity>(EntityId<TEntity> id) where TEntity : IEntity
    {
        if (_entities.TryGetValue(id.Value, out var entity))
        {
            return (TEntity)entity;
        }

        var ingester = new Ingester();

        _store.EventsForEntity(id, ingester);

        var type = (Type)ingester.Values[IEntity.TypeAttribute].Get();

        var createdEntity = (TEntity)Activator.CreateInstance(type, this, id.Value)!;
        _entities.Add(id.Value, createdEntity);
        _values.Add(id.Value, ingester.Values);

        return createdEntity;
    }

    public async ValueTask Add<TEvent>(TEvent entity) where TEvent : IEvent
    {
        _entities.Clear();
        _values.Clear();
        await _store.Add(entity);
    }

    public IAccumulator GetAccumulator<TType, TOwner>(EntityId ownerId, AttributeDefinition<TOwner, TType> attributeDefinition)
        where TOwner : IEntity
    {
        return _values[ownerId][attributeDefinition];
    }

    private struct Ingester() : IEventIngester, IEventContext
    {
        public Dictionary<IAttribute,IAccumulator> Values { get; set; } = new();

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
            var accumulator = GetAccumulator(attr);
            accumulator.Add(value!);
        }

        public void Emit<TOwner, TVal>(EntityId entity, AttributeDefinition<TOwner, TVal> attr, TVal value)
            where TOwner : IEntity
        {
            var accumulator = GetAccumulator(attr);
            accumulator.Add(value!);
        }

        public void Emit<TOwner, TVal>(EntityId<TOwner> entity, MultiEntityAttributeDefinition<TOwner, TVal> attr, EntityId<TVal> value) where TOwner : IEntity where TVal : IEntity
        {
            throw new NotImplementedException();
        }

        public void New<TType>(EntityId<TType> id) where TType : IEntity
        {
            Emit(id.Value, IEntity.TypeAttribute, typeof(TType));
        }
    }
}
