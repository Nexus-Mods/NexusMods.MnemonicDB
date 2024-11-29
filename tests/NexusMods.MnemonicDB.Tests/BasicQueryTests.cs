using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine;
using NexusMods.MnemonicDB.QueryEngine.Facts;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using Xunit.Sdk;
using static NexusMods.MnemonicDB.QueryEngine.QueryPredicates;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

public class BasicQueryTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{
    public static readonly Rule<Fact<EntityId, EntityId>> EnabledFiles = new();

    static BasicQueryTests()
    {
        Declare<EntityId>(out var loadout, out var mod, out var file);
        new Query
            {
                {mod, Mod.Loadout, loadout},
                {file, File.Mod, mod},
            }.AsVariant(EnabledFiles, loadout, file);
    }
    
    
    [Fact]
    public async Task CanQueryDatoms()
    {
        var data = (await InsertLoadouts(1, 1000, 100)).First();

        Declare<EntityId>(out var loadout, out var mod, out var file);
        Declare<Size>(out var fileSize);
        
        var query = new Query
        {
            { EnabledFiles, loadout, file },
            { file, File.Size, fileSize }
        }.AsTableFn(loadout, fileSize);

        for (var i = 0; i < 10; i++)
        {
            GC.Collect();
            var gcBefore = GC.GetTotalMemory(false);
            var sw = Stopwatch.StartNew();
            var results = query(data.Db).Select(t => t.Item1)
                .Aggregate(Size.Zero, (acc, size) => acc + size);
            var gcAfter = GC.GetTotalMemory(false);
            Logger.LogInformation("Query took {ElapsedMilliseconds}ms and {MemoryDelta} bytes", sw.ElapsedMilliseconds, gcAfter - gcBefore);
        }
        //results.Count().Should().Be(100);
    }
    
}
