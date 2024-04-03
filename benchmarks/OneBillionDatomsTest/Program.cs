﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.MneumonicDB;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Storage;
using NexusMods.MneumonicDB.TestModel;
using NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;
using NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(s =>
    {
        s.AddMneumonicDBStorage()
            .AddMneumonicDB()
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

var store = services.GetRequiredService<IDatomStore>();
await store.Sync();

var connection = await Connection.Start(services);

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
    var tx = connection.BeginTransaction();

    for (var j = 0; j < (int)batchSize; j++)
    {
        fileNumber += 1;
        var _ = new File(tx)
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
