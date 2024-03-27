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


    private const int MaxCount = 10000;

    [GlobalSetup]
    public async Task Setup()
    {
        await InitializeAsync();
        var tx = Connection.BeginTransaction();
        var entityIds = new List<EntityId>();
        for (var i = 0; i < MaxCount; i++)
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

        entityIds = entityIds.Select(e => result[e]).ToList();

        var idArray = entityIds.ToArray();

        _readId = idArray.Take(Count).Skip(Count / 2).First();

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
}
