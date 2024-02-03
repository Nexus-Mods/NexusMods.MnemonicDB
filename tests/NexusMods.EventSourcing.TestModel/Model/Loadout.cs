using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

public class Loadout(IEntityContext context, EntityId id) : AEntity(context, id)
{
    public string Name { get; protected set; } = default!;

    public string Description { get; protected set; } = default!;

    public ReadOnlyObservableCollection<Mod> Mods => _mods.Get(this);
    private static readonly MultiEntityAttributeDefinition<Loadout, Mod> _mods = new QueryProperty(nameof(Mods));

}
