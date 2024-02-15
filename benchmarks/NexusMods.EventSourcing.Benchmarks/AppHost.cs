using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.EventSourcing.DatomStore;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Benchmarks;

public static class AppHost
{
    public static IServiceProvider Create()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddDatomStore()
                    .AddEventSourcing()
                    .AddTestModel()
                    .AddSingleton(new DatomStoreSettings()
                    {
                        Path = FileSystem.Shared.GetKnownPath(KnownPath.TempDirectory).Combine(Guid.NewGuid() + ".rocksdb")
                    });
            });

        return builder.Build().Services;
    }

}
