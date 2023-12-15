using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public static class Services
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
    {
        return services
            .AddSingleton<EventSerializer>()
            .AddSingleton<IEntityContext, EntityContext>();
    }

}
