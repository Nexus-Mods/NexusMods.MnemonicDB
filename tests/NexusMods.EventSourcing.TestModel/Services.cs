using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.TestModel;

public static class Services
{
    /// <summary>
    /// Adds all the events to the service collection.
    /// </summary>
    /// <param name="coll"></param>
    /// <returns></returns>
    public static IServiceCollection AddTestModel(this IServiceCollection coll)
    {
        coll.AddEntity<Mod>();
        return coll;
    }

}
