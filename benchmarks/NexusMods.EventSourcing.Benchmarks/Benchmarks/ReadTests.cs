using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;
using Xunit;

// ReSharper disable MemberCanBePrivate.Global

namespace NexusMods.EventSourcing.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ReadTests : ABenchmark
{
    private EntityId _readId;
    private IDb _db = null!;
    private EntityId[] _entityIds = null!;


    private const int MaxCount = 10000;

    [GlobalSetup]
    public async Task Setup()
    {
        await InitializeAsync();
        var tx = Connection.BeginTransaction();
        var entityIds = new List<EntityId>();
        for (var i = 0; i < Count; i++)
        {
            var file = new File(tx)
            {
                Hash = (ulong)i,
                Path = $"C:\\test_{i}.txt",
                Index = (ulong)i
            };
            entityIds.Add(file.Id);
        }
        var result = await tx.Commit();

        _entityIds = entityIds.Select(e => result[e]).ToArray();

        _readId = _entityIds[_entityIds.Length / 2];

        _db = Connection.Db;
    }

    [Params(1, 1000, MaxCount)]
    public int Count { get; set; } = MaxCount;

    [Benchmark]
    public ulong ReadFiles()
    {
        ulong sum = 0;
        sum += _db.Get<File>(_readId).Index;
        return sum;
    }

    [Benchmark]
    public long ReadAll()
    {
        return _db.Get<File>(_entityIds)
            .Sum(e => (long)e.Index);
    }
}
