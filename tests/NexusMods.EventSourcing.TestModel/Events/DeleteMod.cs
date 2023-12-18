using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("5CD171BF-4FFE-40E5-819B-987C48A20DF6")]
[MemoryPackable]
public partial record DeleteMod(EntityId<Mod> ModId, EntityId<Loadout> LoadoutId) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
        Loadout._mods.Remove(context, LoadoutId, ModId);
        Mod._loadout.Unlink(context, ModId);
    }
}
