using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace NexusMods.MnemonicDB.Storage.Tests;

public class WithServiceProviderAttribute : DependencyInjectionDataSourceAttribute<IHost>
{
    public override IHost CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var host = new HostBuilder()
            .ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection();
            })
            .ConfigureServices(s => s.ConfigureServices())
            .Build();
        return host;
    }

    public override object? Create(IHost scope, Type type)
    {
        return scope.Services.GetService(type);
    }
}
