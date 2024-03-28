using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.RocksDbBackend;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;
using NexusMods.EventSourcing.TestModel.ValueSerializers;
using Xunit.DependencyInjection.Logging;
using FileAttributes = NexusMods.EventSourcing.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.EventSourcing.Storage.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEventSourcingStorage()
            .AddSingleton<Backend>()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddValueSerializer<RelativePathSerializer>()
            .AddValueSerializer<HashSerializer>()
            .AddValueSerializer<SizeSerializer>()
            .AddValueSerializer<UriSerializer>()
            .AddAttributeCollection<FileAttributes>()
            .AddAttributeCollection<ModAttributes>();
    }
}
