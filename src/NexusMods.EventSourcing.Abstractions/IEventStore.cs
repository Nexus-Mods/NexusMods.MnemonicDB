using System.Threading.Tasks;

namespace NexusMods.EventSourcing.Abstractions;


public interface IEventStore
{
    public ValueTask Add<T>(T eventEntity) where T : IEvent;

    public ValueTask EventsForEntity<TEntity, TIngester>(EntityId<TEntity> entityId, TIngester ingester)
        where TEntity : IEntity
        where TIngester : IEventIngester;
}
