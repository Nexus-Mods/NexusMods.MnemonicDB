using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using Xunit;

namespace NexusMods.MnemonicDB.Benchmarks.Benchmarks;

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
                services.AddMnemonicDBStorage()
                    .AddRocksDbBackend()
                    .AddMnemonicDB()
                    .AddTestModel()
                    .AddDatomStoreSettings(new DatomStoreSettings
                    {
                        Path = FileSystem.Shared.FromUnsanitizedFullPath("benchmarks" + Guid.NewGuid())
                    });
            });

        _host = builder.Build();
        Connection = Services.GetRequiredService<IConnection>();
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
