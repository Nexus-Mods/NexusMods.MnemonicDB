using Microsoft.Extensions.Hosting;

namespace NexusMods.MnemonicDB.Tests;

public class WithServiceProviderAttribute : DependencyInjectionDataSourceAttribute<IHost>
{
    public override IHost CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        try
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(s => s.ConfigureServices())
                .Build();
            return host;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public override object? Create(IHost scope, Type type)
    {
        return scope.Services.GetService(type);
    }
}
