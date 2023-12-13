using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

public class LoadoutRegistry(IEntityContext context, EntityId id) : AEntity(context, id)
{
    /// <summary>
    /// Gets the instance of the loadout registry from the entity context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static LoadoutRegistry GetInstance(IEntityContext context) =>
        context.Get(EntityId<LoadoutRegistry>.From("10BAE6BA-D5F9-40F4-AF7F-CCA1417C3BB0"));

    /// <summary>
    /// The loadouts in the registry.
    /// </summary>
    public IEnumerable<Loadout> Loadouts => _loadouts.GetAll(this);
    internal static readonly MultiEntityAttributeDefinition<LoadoutRegistry, Loadout> _loadouts = new(nameof(Loadouts));

}
