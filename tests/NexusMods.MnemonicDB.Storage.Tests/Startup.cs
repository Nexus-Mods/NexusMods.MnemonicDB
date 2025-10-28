using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage.Tests.TestAttributes;
using NexusMods.MnemonicDB.TestModel;

namespace NexusMods.MnemonicDB.Storage.Tests;

public static class Startup
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services) =>
        services
            .AddTestModel()
            .AddMnemonicDB()
            .AddSingleton<IAttribute>(Blobs.InKeyBlob)
            .AddSingleton<IAttribute>(Blobs.InValueBlob)
            .AddLogging(builder => builder.AddConsole());
}

