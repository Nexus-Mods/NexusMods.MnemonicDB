using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Tests.DataObjects;

namespace NexusMods.EventSourcing.Tests.Events;

[MemoryPackable]
public partial class CreateCountedEntity : IEvent
{
    public required EntityId<CountedEntity> Id { get; set; }
    public required string Name { get; set; }
    public required int InitialCount { get; set; }

    public ValueTask Apply<T>(T context) where T : IEventContext
    {
        context.AttachEntity(Id, new CountedEntity { Name = Name, Count = InitialCount });
        return ValueTask.CompletedTask;
    }

    public void ModifiedEntities(Action<EntityId> handler)
    {
        handler(Id.Value);
    }
}
