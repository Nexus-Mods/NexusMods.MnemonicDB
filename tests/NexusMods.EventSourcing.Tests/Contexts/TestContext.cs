using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Tests.Contexts;

public class TestContext(ILogger<TestContext> logger) : IEventContext, IEntityContext
{
    private readonly InMemoryEventStore _store = new();
    private readonly Dictionary<EntityId, (IEntity Entity, TransactionId AsOf)> _entities = new();

    private TransactionId _currentTransactionId = TransactionId.From(0);


    public void AttachEntity<TEntity>(EntityId<TEntity> entityId, TEntity entity) where TEntity : IEntity
    {
        if (_entities.ContainsKey(entityId.Value))
        {
            throw new InvalidOperationException($"Entity with id {entityId} already exists in the context");
        }
        _entities.Add(entityId.Value, (entity, _currentTransactionId));
    }

    /// <summary>
    /// Resets the cache of entities, this is useful for testing purposes
    /// </summary>
    public void ResetCache()
    {
        _entities.Clear();
    }

    public ValueTask Transact(IEvent @event)
    {
        var newId = _currentTransactionId.Next();
        _currentTransactionId = newId;
        _store.Add(@event);
        @event.Apply(this);
        logger.LogInformation("Applied {Event} to context txId {Tx}", @event, _currentTransactionId);
        return ValueTask.CompletedTask;
    }

    public ValueTask<T> Retrieve<T>(EntityId<T> entityId) where T : IEntity
    {
        if (_entities.TryGetValue(entityId.Value, out var entity))
        {
            return new ValueTask<T>((T)entity.Entity);
        }

        return LoadEntity(entityId);
    }

    private async ValueTask<T> LoadEntity<T>(EntityId<T> entityId) where T : IEntity
    {
        logger.LogInformation("Loading entity {EntityId} replaying events", entityId);
        var ingester = new Ingester(this);
        await _store.EventsForEntity(entityId, ingester);
        var result = (T)_entities[entityId.Value].Entity;
        logger.LogInformation("Loaded entity {EntityId} with {EventCount} events", entityId, ingester.EventCount);
        return result;
    }

    private class Ingester : IEventIngester
    {
        private readonly TestContext _ctx;
        public int EventCount { get; private set; }

        public Ingester(TestContext ctx)
        {
            _ctx = ctx;
        }

        public async ValueTask Ingest(IEvent @event)
        {
            EventCount++;
            await @event.Apply(_ctx);
        }
    }

    public TransactionId AsOf { get; }
    public ValueTask Advance(TransactionId transactionId)
    {
        throw new NotImplementedException();
    }

    public ValueTask Advance()
    {
        throw new NotImplementedException();
    }
}
