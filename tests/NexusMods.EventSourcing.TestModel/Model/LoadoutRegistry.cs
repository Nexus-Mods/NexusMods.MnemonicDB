using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

public class LoadoutRegistry(IEntityContext context) : AEntity<LoadoutRegistry>(context, SingletonId), ISingletonEntity
{
    public static EntityId<LoadoutRegistry> SingletonId => EntityId<LoadoutRegistry>.From("10BAE6BA-D5F9-40F4-AF7F-CCA1417C3BB0");

    /// <summary>
    /// The loadouts in the registry.
    /// </summary>
    public ReadOnlyObservableCollection<Loadout> Loadouts => _loadouts.Get(this);
    internal static readonly MultiEntityAttributeDefinition<LoadoutRegistry, Loadout> _loadouts = new(nameof(Loadouts));

}
