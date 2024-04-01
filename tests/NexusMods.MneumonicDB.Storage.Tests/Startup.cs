using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Storage.RocksDbBackend;
using NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;
using NexusMods.MneumonicDB.TestModel.ValueSerializers;
using Xunit.DependencyInjection.Logging;
using FileAttributes = NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.MneumonicDB.Storage.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMneumonicDBStorage()
            .AddSingleton<Backend>()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
            .AddValueSerializer<RelativePathSerializer>()
            .AddValueSerializer<HashSerializer>()
            .AddValueSerializer<SizeSerializer>()
            .AddValueSerializer<UriSerializer>()
            .AddAttributeCollection<FileAttributes>()
            .AddAttributeCollection<ModAttributes>()
            .AddAttributeCollection<CollectionAttributes>()
            .AddAttributeCollection<LoadoutAttributes>();
    }
}
