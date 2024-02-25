using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.EventSourcing.Storage.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEventSourcingStorage()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddSingleton<IKvStore, InMemoryKvStore>()
            .AddAttribute<TestAttributes.FileHash>()
            .AddAttribute<TestAttributes.FileName>();
    }
}
