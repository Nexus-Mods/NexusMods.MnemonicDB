using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

[Entity("49C0A910-A022-4AB5-9CF8-3F2730A4BA15", 0)]
public class Loadout(IEntityContext context, EntityId<Loadout> id) : AEntity(context, id)
{
    /// <summary>
    /// The human readable name of the loadout.
    /// </summary>
    public string Name => _name.Get(this);
    internal static readonly ScalarAttribute<Loadout, string> _name = new(nameof(Name));

    /// <summary>
    /// The mods in the loadout.
    /// </summary>
    public ReadOnlyObservableCollection<Mod> Mods => _mods.Get(this);
    internal static readonly MultiEntityAttributeDefinition<Loadout, Mod> _mods = new(nameof(Mods));

    /// <summary>
    /// The Collections contained in the loadout.
    /// </summary>
    public ReadOnlyObservableCollection<Collection> Collections => _collections.Get(this);
    internal static readonly MultiEntityAttributeDefinition<Loadout, Collection> _collections = new(nameof(Collections));
}
