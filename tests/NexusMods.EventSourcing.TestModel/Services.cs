using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Events;
using NexusMods.EventSourcing.TestModel.Model;
using NexusMods.EventSourcing.TestModel.Model.FileTypes;

namespace NexusMods.EventSourcing.TestModel;

public static class Services
{
    /// <summary>
    /// Adds all the events to the service collection.
    /// </summary>
    /// <param name="coll"></param>
    /// <returns></returns>
    public static IServiceCollection AddEvents(this IServiceCollection coll)
    {
        coll.AddEvent<Events.CreateLoadout>()
            .AddEvent<Events.AddMod>()
            .AddEvent<Events.SwapModEnabled>()
            .AddEvent<Events.RenameLoadout>()
            .AddEvent<Events.DeleteMod>()
            .AddEvent<Events.AddCollection>()
            .AddEvent<Events.AddArchive>()
            .AddEvent<Events.AddPluginFile>()
            .AddEvent<Events.AddStaticFile>()
            .AddEntity<Loadout>()
            .AddEntity<Mod>()
            .AddEntity<Collection>()
            .AddEntity<LoadoutRegistry>()
            .AddEntity<ArchiveEntry>()
            .AddEntity<Archive>()
            .AddEntity<StaticFile>()
            .AddEntity<PluginFile>();
        return coll;
    }

}
