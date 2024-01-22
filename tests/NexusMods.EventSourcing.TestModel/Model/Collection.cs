using System.Collections.ObjectModel;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

[Entity("12055587-81CE-44F7-AF2B-6324B0CBB3C4", 0)]
public class Collection : AEntity
{
    public Collection(IEntityContext context, EntityId<Collection> id) : base(context, id) { }

    /// <summary>
    /// Name of the collection
    /// </summary>
    public string Name => _name.Get(this);
    internal static readonly ScalarAttribute<Collection, string> _name = new(nameof(Name));

    /// <summary>
    /// The collection's loadout.
    /// </summary>
    public Loadout Loadout => _loadout.Get(this);
    internal static readonly EntityAttributeDefinition<Collection, Loadout> _loadout = new(nameof(Loadout));

    /// <summary>
    /// The mods in the collection.
    /// </summary>
    public ReadOnlyObservableCollection<Mod> Mods => _mods.Get(this);
    internal static readonly MultiEntityAttributeDefinition<Collection, Mod> _mods = new(nameof(Mods));
}
