using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Events;

namespace NexusMods.EventSourcing;

public static class Services
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
    {
        return services
            .AddSingleton<EventSerializer>()
            .AddEvent<TransactionEvent>()
            .AddSingleton<IEntityContext, EntityContext>();
    }

}
