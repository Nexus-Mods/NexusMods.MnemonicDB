using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

public class Mod(IEntityContext context, EntityId<Mod> id) : AEntity<Mod>(context, id)
{
    public Loadout Loadout => _loadout.Get(this);
    internal static readonly EntityAttributeDefinition<Mod, Loadout> _loadout = new(nameof(Loadout));

    /// <summary>
    /// The human readable name of the mod.
    /// </summary>
    public string Name => _name.Get(this);
    internal static readonly ScalarAttribute<Mod, string> _name = new(nameof(Name));

    /// <summary>
    /// The enabled state of the mod.
    /// </summary>
    public bool Enabled => _enabled.Get(this);
    internal static readonly ScalarAttribute<Mod, bool> _enabled = new(nameof(Enabled));

    /// <summary>
    /// The Collection the mod is in, if any
    /// </summary>
    public Collection Collection => _collection.Get(this);
    internal static readonly EntityAttributeDefinition<Mod, Collection> _collection = new(nameof(Collection));

}
