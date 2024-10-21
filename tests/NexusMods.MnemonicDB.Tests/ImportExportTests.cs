using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.InMemoryBackend;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Tests;

public class ImportExportTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{
    [Fact]
    public async Task CanExportAndImportData()
    {
        await InsertData();
        
        var ms = new MemoryStream();
        await Connection.DatomStore.ExportAsync(ms);
        
        Logger.LogInformation("Exported {0} bytes", ms.Length);
        
        var datomStore = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>()!,
            Config, new Backend(), bootstrap: false);
        
        ms.Position = 0;
        await datomStore.ImportAsync(ms);
        
        foreach (var index in Enum.GetValues<IndexType>())
        {
            if (index == IndexType.None)
                continue;
            
            var slice = SliceDescriptor.Create(index);
            var setA = Connection.DatomStore.GetSnapshot().Datoms(slice);
            var setB = datomStore.GetSnapshot().Datoms(slice);

            var setDiff = setB.Except(setA).ToArray();
            
            setB.Count.Should().Be(setA.Count);
            foreach (var (a, b) in setA.Zip(setB))
            {
                a.Should().BeEquivalentTo(b);
            }
        }

    }

    private async Task InsertData()
    {
        using var tx = Connection.BeginTransaction();

        var loadout = new Loadout.New(tx)
        {
            Name = "Test Loadout",
        };

        foreach (var modIdx in Enumerable.Range(0, 10))
        {
            var mod = new Mod.New(tx)
            {
                Name = $"Mod{modIdx}",
                Source = new Uri($"http://somesite.com/Mod{modIdx}"),
                LoadoutId = loadout,
            };
            
            foreach (var fileIdx in Enumerable.Range(0, 10))
            {
                _ = new TestModel.File.New(tx)
                {
                    Path = $"File{fileIdx}",
                    ModId = mod,
                    Size = Size.From((ulong)fileIdx),
                    Hash = Hash.From((ulong)(0xDEADBEEF + fileIdx)),
                };
            }
        }

        var txResult = await tx.Commit();
    }
}
