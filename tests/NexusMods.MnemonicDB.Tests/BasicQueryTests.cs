using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using Xunit.Sdk;
using static NexusMods.MnemonicDB.QueryEngine.QueryPredicates;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

public class BasicQueryTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{
    [Fact]
    public async Task CanQueryDatoms()
    {
        var data = (await InsertLoadouts(1, 10, 100)).First();
        
        var mod = LVar.Create<EntityId>("mod");
        var file = LVar.Create<EntityId>("file");

        var query = new Query<EntityId, Size>(out var loadout, out var fileSize)
        {
            Db(mod, Mod.Loadout, loadout),
            Db(file, File.Mod, mod),
            Db(file, File.Size, fileSize)
        };

        for (int i = 0; i < 2; i++)
        {
            GC.Collect();
            var gcBefore = GC.GetTotalMemory(false);
            var sw = Stopwatch.StartNew();
            var results = query.TableFn()(data.Db)
                .Select(t => t.Item2)
                .Aggregate(Size.Zero, (acc, size) => acc + size);
            var gcAfter = GC.GetTotalMemory(false);
            Logger.LogInformation("Query took {ElapsedMilliseconds}ms and {MemoryDelta} bytes", sw.ElapsedMilliseconds, gcAfter - gcBefore);
        }
        //results.Count().Should().Be(100);
    }
    
}
