using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage;
using NexusMods.EventSourcing.TestModel;

namespace NexusMods.EventSourcing.Benchmarks;

public static class AppHost
{
    public static IServiceProvider Create()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddEventSourcingStorage()
                    .AddSingleton<IKvStore, InMemoryKvStore>()
                    .AddEventSourcing()
                    .AddTestModel();
            });

        return builder.Build().Services;
    }

    public static async Task<IConnection> CreateConnection(IServiceProvider provider)
    {
        return await Connection.Start(provider);
    }

}
