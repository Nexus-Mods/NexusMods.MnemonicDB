using NexusMods.EventSourcing.Abstractions;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.EventSourcing.TestModel.Model;

public class Mod(IEntityContext context, EntityId id) : AEntity(context, id)
{

    public Loadout Loadout => _loadout.GetEntity(this);
    internal static readonly EntityAttributeDefinition<Mod, Loadout> _loadout = new(nameof(Loadout));

    /// <summary>
    /// The human readable name of the mod.
    /// </summary>
    public string Name => _name.Get(this);
    internal static readonly AttributeDefinition<Mod, string> _name = new(nameof(Name));

    /// <summary>
    /// The enabled state of the mod.
    /// </summary>
    public bool Enabled => _enabled.Get(this);
    internal static readonly AttributeDefinition<Mod, bool> _enabled = new(nameof(Enabled));
}
