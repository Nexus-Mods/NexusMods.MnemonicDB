using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

[Entity("4FE4CDC6-7A14-4FBC-BCF4-8B39D93F8EE4", 0)]
public class LoadoutRegistry(IEntityContext context) : AEntity(context, SingletonId.Id), ISingletonEntity
{
    /// <summary>
    /// A singleton id for the loadout registry
    /// </summary>
    public static EntityId<LoadoutRegistry> SingletonId => EntityId<LoadoutRegistry>.From("10BAE6BA-D5F9-40F4-AF7F-CCA1417C3BB0");

    /// <summary>
    /// The loadouts in the registry.
    /// </summary>
    public ReadOnlyObservableCollection<Loadout> Loadouts => _loadouts.Get(this);
    internal static readonly MultiEntityAttributeDefinition<LoadoutRegistry, Loadout> _loadouts = new(nameof(Loadouts));


    public Dictionary<string, EntityId<Loadout>> LoadoutNames => _loadoutNames.Get(this);
    internal static readonly IndexedMultiEntityAttributeDefinition<LoadoutRegistry, string, Loadout> _loadoutNames = new(nameof(LoadoutNames));

}
