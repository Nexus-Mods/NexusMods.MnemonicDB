using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

public class Loadout(IEntityContext context, EntityId id) : AEntity(context, id)
{
    /// <summary>
    /// The human readable name of the loadout.
    /// </summary>
    public string Name => _name.Get(this);
    internal static readonly AttributeDefinition<Loadout, string> _name = new(nameof(Name));

    /// <summary>
    /// The mods in the loadout.
    /// </summary>
    public IEnumerable<Mod> Mods => _mods.GetAll(this);
    internal static readonly MultiEntityAttributeDefinition<Loadout, Mod> _mods = new(nameof(Mods));

}
