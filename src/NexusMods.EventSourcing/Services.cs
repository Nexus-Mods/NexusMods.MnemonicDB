using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.EventSourcing;

public static class Services
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
    {
        return services.AddSingleton<EventSerializer>();
    }

}
