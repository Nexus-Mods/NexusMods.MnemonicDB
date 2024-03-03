using System;
using Microsoft.Extensions.Hosting;
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
                    .AddEventSourcing()
                    .AddTestModel();
            });

        return builder.Build().Services;
    }

}
