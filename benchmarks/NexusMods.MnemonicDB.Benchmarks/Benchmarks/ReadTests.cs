using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using Xunit.Sdk;

// ReSharper disable MemberCanBePrivate.Global

namespace NexusMods.MnemonicDB.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ReadTests : ABenchmark
{
    private const int MaxCount = 10000;
    private IDb _db = null!;
    private EntityId[] _entityIds = null!;
    private EntityId _readId;
    private IFile[] _preLoaded = null!;
    private EntityId _modId;

    [Params(1, 1000, MaxCount)] public int Count { get; set; } = MaxCount;

    [GlobalSetup]
    public async Task Setup()
    {
        await InitializeAsync();
        var tx = Connection.BeginTransaction();
        var entityIds = new List<EntityId>();

        var tmpMod = tx.New<IMod>();
        tmpMod.Name = "TestMod";
        tmpMod.Source = new Uri("https://www.nexusmods.com");

        for (var i = 0; i < Count; i++)
        {
            var file = tx.New<IFile>();
            file.Hash = Hash.From((ulong)i);
            file.Path = $"C:\\test_{i}.txt";
            file.Size = Size.From((ulong)i);
            file.Mod = tmpMod;
            entityIds.Add(file.Id);
        }

        var result = await tx.Commit();

        _modId = result[tmpMod.Id];

        _entityIds = entityIds.Select(e => result[e]).ToArray();

        _readId = _entityIds[_entityIds.Length / 2];

        _db = Connection.Db;

        _preLoaded = _entityIds.Select(id => _db.Get<IFile>(id)).ToArray();
    }

    [Benchmark]
    public ulong ReadFiles()
    {
        ulong sum = 0;
        sum += _db.Get<IFile>(_readId).Size.Value;
        return sum;
    }

    [Benchmark]
    public ulong ReadProperty()
    {
        return _preLoaded[0].Size.Value;
    }

    [Benchmark]
    public long ReadAll()
    {
        return _entityIds.Select(id => _db.Get<IFile>(id))
            .Sum(e => (long)e.Size.Value);
    }

    [Benchmark]
    public long ReadAllPreloaded()
    {
        return _preLoaded
            .Sum(e => (long)e.Size.Value);
    }

    [Benchmark]
    public ulong ReadAllFromMod()
    {
        var mod = _db.Get<IMod>(_modId);
        ulong sum = 0;
        for (var i = 0; i < mod.Files.Count(); i++)
        {
            throw new NotImplementedException();
            //var file = mod.Files[i];
            //sum += file.Size.Value;
        }

        return sum;
    }
}
