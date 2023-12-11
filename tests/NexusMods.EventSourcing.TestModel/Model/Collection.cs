using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.EventSourcing.Abstractions;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.EventSourcing.TestModel.Model;

public class Collection
{
    public Loadout Loadout { get; internal set; } = null!;

    internal SourceCache<Mod, EntityId> _mods = new(x => x.Id);

    private ReadOnlyObservableCollection<Mod> _modsConnected = null!;
    public ReadOnlyObservableCollection<Mod> Mods { get; internal set; } = null!;

    [Reactive]
    public string Name { get; internal set; } = string.Empty;

    public Collection()
    {
        _mods.Connect()
            .Bind(out _modsConnected)
            .Subscribe();
    }
}
