using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("95C3D0AF-EBCA-4DA8-ACAE-144E66F48A50")]
[MemoryPackable]
public partial class RenameLoadout : IEvent
{
    public required EntityId<Loadout> Id { get; init; }
    public required string Name { get; init; }

    public ValueTask Apply<T>(T context) where T : IEventContext
    {
        context.Emit(Id, Loadout._name, Name);
        return ValueTask.CompletedTask;
    }

    public static RenameLoadout Create(EntityId<Loadout> id, string name) => new() { Id = id, Name = name };
}
