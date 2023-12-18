using DynamicData;
using MemoryPack;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("7DC8F80B-50B6-43B7-B805-43450E9F0C2B")]
[MemoryPackable]
public partial record AddMod(string Name, bool Enabled, EntityId<Mod> ModId, EntityId<Loadout> LoadoutId) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
        IEntity.TypeAttribute.New(context, ModId);
        Mod._name.Set(context, ModId, Name);
        Mod._enabled.Set(context, ModId, Enabled);
        Mod._loadout.Link(context, ModId, LoadoutId);
        Loadout._mods.Add(context, LoadoutId, ModId);
    }

    /// <summary>
    /// Creates a event that adds a new mod to the given loadout giving it the given name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="loadoutId"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public static AddMod Create(string name, EntityId<Loadout> loadoutId, bool enabled = true)
        => new(name, enabled, EntityId<Mod>.NewId(), loadoutId);
}
