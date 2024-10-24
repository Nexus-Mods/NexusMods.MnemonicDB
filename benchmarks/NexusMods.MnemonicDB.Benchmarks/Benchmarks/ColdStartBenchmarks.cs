using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ColdStartBenchmarks : ABenchmark
{
    [GlobalSetup]
    public async ValueTask GlobalSetup()
    {
        await InitializeAsync();
        await InsertData();
    }

    private async Task InsertData()
    {
        foreach (var loadout in Enumerable.Range(0, 10))
        {
            using var tx = Connection.BeginTransaction();
            var loadoutEntity = new Loadout.New(tx)
            {
                Name = $"Loadout {loadout}"
            };
            foreach (var mod in Enumerable.Range(0, 1000))
            {
                var modEntity = new Mod.New(tx)
                {
                    Name = $"Mod {mod}",
                    Source = new System.Uri($"http://mod{mod}.com"),
                    LoadoutId = loadoutEntity,
                    OptionalHash = Hashing.xxHash3.Hash.FromLong(0)
                };
                foreach (var file in Enumerable.Range(0, 100))
                {
                    _ = new File.New(tx)
                    {
                        Path = $"File {file}",
                        ModId = modEntity,
                        Size = Size.FromLong(file),
                        Hash = Hashing.xxHash3.Hash.FromLong(file)
                    };
                }
            }
            await tx.Commit();
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        Connection.Db.ClearIndexCache();
    }

    [Benchmark]
    public Size TotalSizeEAVT()
    {
        var loadout = Loadout.FindByName(Connection.Db, "Loadout 5").First();
        var totalSize = Size.FromLong(0);
        
        foreach (var mod in loadout.Mods)
        {
            foreach (var file in mod.Files)
            {
                totalSize += file.Size;
            }
        }

        return totalSize;
    }

    
    [Benchmark]
    public Size TotalSizeAEVT()
    {
        var loadoutId = Connection.Db.Datoms(Loadout.Name, "Loadout 5").First();
        var modIds = Connection.Db.Datoms(Mod.LoadoutId, loadoutId.E).Select(e => e.E).ToHashSet();
        var fileIds = Connection.Db.Datoms(File.ModId).ToLookup(d => ValueTag.Reference.Read<EntityId>(d.ValueSpan), d => d.E);
        var fileSizes = Connection.Db.Datoms(File.Size)
            .ToDictionary(d => d.E, d => Size.From(ValueTag.UInt64.Read<ulong>(d.ValueSpan)));
        
        var totalSize = Size.FromLong(0);
        foreach (var modId in modIds)
        {
            foreach (var fileId in fileIds[modId])
            {
                totalSize += fileSizes[fileId];
            }
        }
        return totalSize;
    }
}
