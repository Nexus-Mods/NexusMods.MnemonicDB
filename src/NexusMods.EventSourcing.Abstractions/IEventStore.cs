using System.Threading.Tasks;

namespace NexusMods.EventSourcing.Abstractions;


public interface IEventStore
{
    public TransactionId Add<T>(T eventEntity) where T : IEvent;

    public void EventsForEntity<TIngester>(EntityId entityId, TIngester ingester)
        where TIngester : IEventIngester;
}
