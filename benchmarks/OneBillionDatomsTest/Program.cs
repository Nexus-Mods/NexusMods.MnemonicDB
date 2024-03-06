using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage;
using NexusMods.EventSourcing.Storage.RocksDb;
using NexusMods.EventSourcing.Storage.Serializers;
using NexusMods.EventSourcing.Storage.Tests;
using NexusMods.EventSourcing.TestModel;
using NexusMods.EventSourcing.TestModel.Model;
using NexusMods.Paths;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(s =>
    {
        s.AddEventSourcingStorage()
            .AddEventSourcing()
            .AddTestModel()
            .AddSingleton<RocksDbKvStoreConfig>(_ => new RocksDbKvStoreConfig()
            {
                Path = FileSystem.Shared.FromUnsanitizedFullPath(@"c:\tmp\billionDatomsTest" + Guid.NewGuid())
            })
            .AddSingleton<IKvStore, RocksDbKvStore>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Error);
    })
    .Build();

var services = host.Services;

var store = services.GetRequiredService<IDatomStore>();
await store.Sync();

var connection = await Connection.Start(services);

var settings = services.GetRequiredService<DatomStoreSettings>();
//settings.MaxInMemoryDatoms = 1024 * 32;

ulong batchSize = 1024;
ulong datomCount = 10_000_00;
ulong entityCount = datomCount / 3; // 3 attributes per entity
var batches = entityCount / batchSize;



Console.WriteLine($"Inserting {entityCount} entities in {batches} batches of 1024 entities each");

var globalSw = Stopwatch.StartNew();
ulong fileNumber = 0;
for (ulong i = 0; i < batches; i++)
{
    var tx = connection.BeginTransaction();

    for (var j = 0; j < (int)batchSize; j++)
    {
        fileNumber += 1;
        var file = new File(tx)
        {
            Path = $"c:\\test_{i}_{j}.txt",
            Hash = fileNumber,
            Index = entityCount - fileNumber
        };
    }
    var sw = Stopwatch.StartNew();
    await tx.Commit();

    var perSecond = (int)((batchSize * i * 3) / globalSw.Elapsed.TotalSeconds);

    Console.WriteLine($"({i}/{batches}) Elapsed: {sw.ElapsedMilliseconds}ms - Datoms per second: {perSecond}");
}


