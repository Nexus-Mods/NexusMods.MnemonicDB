using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

public class Loadout : IEntity
{
    public EntityId Id { get; internal set; }

    public string Name { get; internal set; } = string.Empty;

    internal SourceCache<Mod, EntityId> _mods = new(x => x.Id);
}
