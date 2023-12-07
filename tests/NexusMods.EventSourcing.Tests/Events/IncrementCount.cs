using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Tests.DataObjects;

namespace NexusMods.EventSourcing.Tests.Events;

[MemoryPackable]
public partial class IncrementCount : IEvent
{
    public required EntityId<CountedEntity> Entity { get; set; }
    public required int Increment { get; set; }

    public async ValueTask Apply<T>(T context) where T : IEventContext
    {
        var entity = await context.Retrieve(Entity);
        entity.Count += Increment;
    }

    public void ModifiedEntities(Action<EntityId> handler)
    {
        handler(Entity.Value);
    }
}
