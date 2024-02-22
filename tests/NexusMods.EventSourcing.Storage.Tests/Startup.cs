using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Tests;

public class Startup
{


    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEventSourcingStorage()
            .AddSingleton<IKvStore, InMemoryKvStore>()
            .AddAttribute<TestAttributes.FileHash>();
    }
}
