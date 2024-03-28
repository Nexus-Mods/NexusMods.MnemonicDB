using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.TestModel.ComplexModel.ReadModels;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

// ReSharper disable MemberCanBePrivate.Global

namespace NexusMods.MneumonicDB.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ReadTests : ABenchmark
{
    private const int MaxCount = 10000;
    private IDb _db = null!;
    private EntityId[] _entityIds = null!;
    private EntityId _readId;

    [Params(1, 1000, MaxCount)] public int Count { get; set; } = MaxCount;

    [GlobalSetup]
    public async Task Setup()
    {
        await InitializeAsync();
        var tx = Connection.BeginTransaction();
        var entityIds = new List<EntityId>();

        var modId = new Mod(tx)
        {
            Name = "TestMod",
            Source = new Uri("https://www.nexusmods.com"),
            LoadoutId = EntityId.From(1)
        };

        for (var i = 0; i < Count; i++)
        {
            var file = new File(tx)
            {
                Hash = Hash.From((ulong)i),
                Path = $"C:\\test_{i}.txt",
                Size = Size.From((ulong)i),
                ModId = modId.Id,
            };
            entityIds.Add(file.Id);
        }

        var result = await tx.Commit();

        _entityIds = entityIds.Select(e => result[e]).ToArray();

        _readId = _entityIds[_entityIds.Length / 2];

        _db = Connection.Db;
    }

    [Benchmark]
    public ulong ReadFiles()
    {
        ulong sum = 0;
        sum += _db.Get<File>(_readId).Size.Value;
        return sum;
    }

    [Benchmark]
    public long ReadAll()
    {
        return _db.Get<File>(_entityIds)
            .Sum(e => (long)e.Size.Value);
    }
}
