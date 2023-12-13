using System.Threading;
using System.Threading.Tasks;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public class EntityContext : IEntityContext
{
    public TEntity Get<TEntity>(EntityId<TEntity> id) where TEntity : IEntity
    {
        throw new System.NotImplementedException();
    }

    public ValueTask Add<TEvent>(TEvent entity) where TEvent : IEvent
    {
        throw new System.NotImplementedException();
    }

    public IAccumulator GetAccumulator<TType, TOwner>(EntityId ownerId, AttributeDefinition<TOwner, TType> attributeDefinition) where TOwner : IEntity
    {
        throw new System.NotImplementedException();
    }
}
