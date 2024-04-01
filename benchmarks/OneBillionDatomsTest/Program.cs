using System;
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

async Task<(TimeSpan Time, int loaded)> ReadCheckpoint(IDb db)
{
    var tasks = new List<Task<(TimeSpan Time, int loaded)>>();


    for (int i = 0; i < 1; i++)
    {
        var task = Task.Run(() =>
        {
            var sw = Stopwatch.StartNew();
            using var iterator = db.Iterate(IndexType.TxLog);

            var count = 0;
            var scanned = 0;
            foreach (var itm in iterator.SeekLast().Reverse().Resolve())
            {
                if (count == 1024 * 128) break;
                if (itm is FileAttributes.Hash.ReadDatom hashDatom)
                {
                    var file = db.Get<File>(hashDatom.E);
                    count++;
                }

                scanned++;
            }

            return (sw.Elapsed, (count * 4) + scanned);
        });
        tasks.Add(task);
    }

    var results = await Task.WhenAll(tasks);

    return (results.Max(r => r.Time), results.Sum(r => r.loaded));

}

var checkPointTask = ReadCheckpoint(connection.Db);
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
        var checkpointStatus = await checkPointTask;
        var estimatedRemaining = (batches - i) * (globalSw.Elapsed.TotalSeconds / i);
        Console.WriteLine(
            $"({i}/{batches}) Elapsed: {globalSw.Elapsed} - Datoms per second: {perSecond} - ETA: {TimeSpan.FromSeconds(estimatedRemaining)}");
        Console.WriteLine(" - Read Checkpoint: " + checkpointStatus.loaded + " datoms in " + checkpointStatus.Time.Milliseconds + "ms");
        lastPrint = DateTime.UtcNow;

        checkPointTask = ReadCheckpoint(connection.Db);
    }
}


Console.WriteLine(
    $"Elapsed: {globalSw.ElapsedMilliseconds}ms - Datoms per second: {datomCount / globalSw.Elapsed.TotalSeconds}");
