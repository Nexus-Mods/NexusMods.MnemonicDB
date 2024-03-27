using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.InMemoryBackend;
using NexusMods.EventSourcing.TestModel;
using NexusMods.Paths;
using Xunit;

namespace NexusMods.EventSourcing.Benchmarks.Benchmarks;

public class ABenchmark : IAsyncLifetime
{
    private IHost _host = null!;
    protected IConnection Connection = null!;

    public IServiceProvider Services => _host.Services;

    public async Task InitializeAsync()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddEventSourcingStorage()
                    .AddRocksDbBackend()
                    .AddEventSourcing()
                    .AddTestModel()
                    .AddDatomStoreSettings(new DatomStoreSettings
                    {
                        Path = FileSystem.Shared.FromUnsanitizedFullPath("benchmarks" + Guid.NewGuid())
                    });
            });

        _host = builder.Build();
        Connection = await NexusMods.EventSourcing.Connection.Start(Services);
    }

    public async Task DisposeAsync()
    {
        var path = Services.GetRequiredService<DatomStoreSettings>().Path;
        await _host.StopAsync();
        _host.Dispose();

        if (path.DirectoryExists())
            path.DeleteDirectory();
        if (path.FileExists)
            path.Delete();
    }
}
