using DynamicData;
using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("63A4CB90-27E2-468A-BE94-CB01A38D8C09")]
[MemoryPackable]
public partial class CreateLoadout : IEvent
{
    public required string Name { get; init; }

    public required EntityId<Loadout> Id { get; init; }


    public async ValueTask Apply<T>(T context) where T : IEventContext
    {
        context.New(Id);
        context.Emit(Id, Loadout._name, Name);
    }

    public static CreateLoadout Create(string name) => new() { Name = name, Id = EntityId<Loadout>.NewId() };

    public void ModifiedEntities(Action<EntityId> handler)
    {
        handler(Id.Value);
    }
}
