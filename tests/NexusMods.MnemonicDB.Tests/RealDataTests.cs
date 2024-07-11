using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.RealisticTestData;
using NexusMods.MnemonicDB.RealisticTestData.Models;
using NexusMods.MnemonicDB.Storage;
using NexusMods.Paths;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.MnemonicDB.Tests;

public class RealDataTests : IAsyncLifetime
{
    private readonly IHost _host;
    private readonly IServiceProvider _services;
    private readonly IConnection _conn;
    private readonly ILogger<RealDataTests> _logger;
    private readonly AbsolutePath _path;
    private Loadout.ReadOnly _loadout;

    public RealDataTests(IServiceProvider upper)
    {
        
        _logger = upper.GetRequiredService<ILogger<RealDataTests>>();
        _path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("tests_MnemonicDB")
            .Combine(Guid.NewGuid().ToString())
            .Combine("MnemonicDB");
        
        var host = new HostBuilder()
            .ConfigureServices(s =>
            {

                
                _path.CreateDirectory();
                
                s.AddArchiveModel()
                    .AddModModel()
                    .AddLoadoutModel()
                    .AddExtractedFileModel()
                    .AddSingleton(upper.GetRequiredService<ITestOutputHelperAccessor>())
                    .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Debug))
                    .AddMnemonicDB()
                    .AddMnemonicDBStorage()
                    .AddRocksDbBackend()
                    .AddSingleton(_ => new DatomStoreSettings
                    {
                        Path = _path
                    });
            });
        _host = host.Build();
        _services = _host.Services;
        _conn = _services.GetRequiredService<IConnection>();
    }
    
    [Fact]
    public async Task CanImport()
    {


        await Verify(_loadout.Mods.OrderBy(m => m.Priority)
            .Select(m => m.Name));

    }

    public async Task InitializeAsync()
    {
        _loadout = await Root.Import(_conn);
    }

    public Task DisposeAsync()
    {
        _host.Dispose();
        _path.DeleteDirectory();
        return Task.CompletedTask;
    }
}
