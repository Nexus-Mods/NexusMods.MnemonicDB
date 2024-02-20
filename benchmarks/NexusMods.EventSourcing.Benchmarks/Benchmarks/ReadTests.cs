using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ReadTests
{
    private readonly IConnection _connection;
    private List<EntityId> _entityIdsAscending = null!;
    private List<EntityId> _entityIdsDescending = null!;
    private List<EntityId> _entityIdsRandom = null!;

    public ReadTests()
    {
        var services = AppHost.Create();

        _connection = services.GetRequiredService<IConnection>();
    }

    public const int MaxCount = 10000;

    [GlobalSetup]
    public void Setup()
    {
        var tx = _connection.BeginTransaction();
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
        var result = tx.Commit();

        entityIds = entityIds.Select(e => result[e]).ToList();
        _entityIdsAscending = entityIds.OrderBy(id => id.Value).ToList();
        _entityIdsDescending = entityIds.OrderByDescending(id => id.Value).ToList();

        var idArray = entityIds.ToArray();
        Random.Shared.Shuffle(idArray);
        _entityIdsRandom = idArray.ToList();
    }


    [Params(1, 1000, MaxCount)]
    public int Count { get; set; } = MaxCount;

    public enum SortOrder
    {
        Ascending,
        Descending,
        Random
    }


    //[Params(SortOrder.Ascending, SortOrder.Descending, SortOrder.Random)]
    public SortOrder Order { get; set; } = SortOrder.Descending;

    public List<EntityId> Ids => Order switch
    {
        SortOrder.Ascending => _entityIdsAscending,
        SortOrder.Descending => _entityIdsDescending,
        SortOrder.Random => _entityIdsRandom,
        _ => throw new ArgumentOutOfRangeException()
    };

    [Benchmark]
    public ulong ReadFiles()
    {
        var db = _connection.Db;
        ulong sum = 0;
        foreach (var itm in db.Get<File>(Ids.Take(Count)))
        {
            sum += itm.Index;
        }
        return (ulong)sum;
    }

}
