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
    public required EntityId<Loadout> LoadoutId { get; init; }

    public async ValueTask Apply<T>(T context) where T : IEventContext
    {
        context.New(Id);
        context.Emit(Id, Mod._name, Name);
        context.Emit(Id, Mod._enabled, Enabled);
        context.Emit(Id, Mod._loadout, LoadoutId);
        context.Emit(LoadoutId, Loadout._mods, Id);
    }

    public static AddMod Create(string name, EntityId<Loadout> loadoutId) => new() { Name = name, Enabled = true, Id = EntityId<Mod>.NewId(), LoadoutId = loadoutId };
}
