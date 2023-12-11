using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

public class LoadoutRegistry : IEntity
{
    internal readonly SourceCache<Loadout, EntityId> _loadouts = new(x => x.Id);

    private ReadOnlyObservableCollection<Loadout> _loadoutsConnected;
    public ReadOnlyObservableCollection<Loadout> Loadouts => _loadoutsConnected;

    public LoadoutRegistry()
    {
        _loadouts.Connect()
            .Bind(out _loadoutsConnected)
            .Subscribe();
    }

    public static EntityId<LoadoutRegistry> StaticId = new(EntityId.From(Guid.Parse("7F3E3745-51B9-44CB-BBDA-B1555191330E")));

    public EntityId Id { get; } = StaticId.Value;
}
