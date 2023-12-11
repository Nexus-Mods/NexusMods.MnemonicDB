using DynamicData;
using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("7DC8F80B-50B6-43B7-B805-43450E9F0C2B")]
[MemoryPackable]
public partial class AddMod : IEvent
{
    public required string Name { get; init; } = string.Empty;
    public required bool Enabled { get; init; } = true;
    public required EntityId<Mod> Id { get; init; }
    public required EntityId<Loadout> Loadout { get; init; }

    public async ValueTask Apply<T>(T context) where T : IEventContext
    {
        var loadout = await context.Retrieve(Loadout);
        var mod = new Mod
        {
            Id = Id.Value,
            Name = Name,
            Enabled = Enabled,
        };
        loadout._mods.AddOrUpdate(mod);
        context.AttachEntity(Id, mod);

    }

    public void ModifiedEntities(Action<EntityId> handler)
    {
        handler(Id.Value);
        handler(Loadout.Value);
    }
}
