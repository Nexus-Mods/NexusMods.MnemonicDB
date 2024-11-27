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
        var data = (await InsertLoadouts(1, 1000, 100)).First();
        
        var loadout = LVar.Create<EntityId>("loadout");
        var mod = LVar.Create<EntityId>("mod");
        var file = LVar.Create<EntityId>("file");
        var fileSize = LVar.Create<Size>("fileSize");

        var query = new Query
        {
            {mod, Mod.Loadout, loadout},
            {file, File.Mod, mod},
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
