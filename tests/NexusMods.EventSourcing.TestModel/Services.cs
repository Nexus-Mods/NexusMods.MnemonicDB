using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Events;

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
        coll.AddEvent<Events.CreateLoadout>();
        coll.AddEvent<Events.AddMod>();
        coll.AddEvent<Events.SwapModEnabled>();
        coll.AddEvent<Events.RenameLoadout>();
        coll.AddEvent<Events.DeleteMod>();
        return coll;
    }

}
