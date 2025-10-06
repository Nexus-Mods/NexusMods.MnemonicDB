using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace NexusMods.MnemonicDB.Tests;

public class WithServiceProviderAttribute : DependencyInjectionDataSourceAttribute<IHost>
{
    public override IHost CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Avoid Host.CreateDefaultBuilder because it enables reloadOnChange for configuration
        // sources, which creates FileSystemWatchers and can exhaust inotify instances on Linux.
        var host = new HostBuilder()
            .ConfigureAppConfiguration((_, cfg) =>
            {
                // Minimal configuration; do not enable reloadOnChange or file watchers.
                cfg.AddInMemoryCollection();
            })
            .ConfigureServices(s => s.ConfigureServices())
            .ConfigureLogging(b => { /* logging configured via services */ })
            .Build();
        return host;
    }

    public override object? Create(IHost scope, Type type)
    {
        return scope.Services.GetService(type);
    }
}
