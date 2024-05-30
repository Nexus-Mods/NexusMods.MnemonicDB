using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(s =>
    {
        s.AddMnemonicDBStorage()
            .AddMnemonicDB()
            .AddRocksDbBackend()
            .AddTestModel()
            .AddSingleton<DatomStoreSettings>(_ => new DatomStoreSettings
            {
                Path = FileSystem.Shared.FromUnsanitizedFullPath(@"billionDatomsTest" + Guid.NewGuid())
            });
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Error);
    })
    .Build();

var services = host.Services;

var connection = services.GetRequiredService<IConnection>();
await ((IHostedService)connection).StartAsync(CancellationToken.None);

ulong batchSize = 1024;
ulong datomCount = 1_000_000_000;
var entityCount = datomCount / 3; // 3 attributes per entity
var batches = entityCount / batchSize;


Console.WriteLine($"Inserting {entityCount} entities in {batches} batches of {batchSize} datoms each");

var globalSw = Stopwatch.StartNew();
ulong fileNumber = 0;
var lastPrint = DateTime.UtcNow;



for (ulong i = 0; i < batches; i++)
{
    using var tx = connection.BeginTransaction();

    for (var j = 0; j < (int)batchSize; j++)
    {
        fileNumber += 1;
        var _ = new File.New(tx)
        {
            Path = $"c:\\test_{i}_{j}.txt",
            Hash = Hash.From(fileNumber % 0xFFFF),
            Size = Size.From(fileNumber),
            ModId = EntityId.From(1)
        };
    }

    await tx.Commit();

    var perSecond = (int)(batchSize * i * 3 / globalSw.Elapsed.TotalSeconds);

    if (DateTime.UtcNow - lastPrint > TimeSpan.FromSeconds(1))
    {
        var estimatedRemaining = (batches - i) * (globalSw.Elapsed.TotalSeconds / i);
        Console.WriteLine(
            $"({i}/{batches}) Elapsed: {globalSw.Elapsed} - Datoms per second: {perSecond} - ETA: {TimeSpan.FromSeconds(estimatedRemaining)}");
        lastPrint = DateTime.UtcNow;
    }
}


Console.WriteLine(
    $"Elapsed: {globalSw.ElapsedMilliseconds}ms - Datoms per second: {datomCount / globalSw.Elapsed.TotalSeconds}");
