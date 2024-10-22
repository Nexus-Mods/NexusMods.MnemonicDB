using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[MaxIterationCount(20)]
public class ReadThenWriteBenchmarks : ABenchmark
{

    private EntityId _modId;

    [GlobalSetup]
    public async Task Setup()
    {
        await InitializeAsync();
        
        using var tx = Connection.BeginTransaction();
        
        var loadout = new Loadout.New(tx)
        {
            Name = "My Loadout"
        };

        for (int i = 0; i < 90; i++)
        {
            var mod = new Mod.New(tx)
            {
                Name = $"Mod {i}",
                Source = new System.Uri($"http://mod{i}.com"),
                LoadoutId = loadout,
                OptionalHash = Hash.FromLong(0)
            };
            
            _modId = mod.Id;

            for (int j = 0; j < 1000; j++)
            {
                var file = new File.New(tx)
                {
                    Path = $"File {j}",
                    ModId = mod,
                    Size = Size.FromLong(j),
                    Hash = Hash.FromLong(j)
                };
            }
        }
        
        var result = await tx.Commit();
        _modId = result[_modId];
    }
    
    [Benchmark]
    public async Task<ulong> ReadThenWrite()
    {
        using var tx = Connection.BeginTransaction();
        var mod = Mod.Load(Connection.Db, _modId);
        var oldHash = mod.OptionalHash;
        tx.Add(_modId, Mod.OptionalHash, Hash.From(oldHash.Value + 1));
        var nextdb = await tx.Commit();
        
        var loadout = Loadout.Load(Connection.Db, mod.LoadoutId);


        ulong totalSize = 0;
        foreach (var mod2 in loadout.Mods)
        {
            foreach (var file in mod2.Files)
            {
                totalSize += file.Size.Value;
            }
        }
        return totalSize;
    }

}
