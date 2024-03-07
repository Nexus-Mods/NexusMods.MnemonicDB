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
public class ReadTests : IAsyncLifetime
{
    private IConnection _connection = null!;
    private List<EntityId> _entityIdsAscending = null!;
    private List<EntityId> _entityIdsDescending = null!;
    private List<EntityId> _entityIdsRandom = null!;
    private readonly IServiceProvider _services;
    private EntityId _readId;
    private IDb _db = null!;

    public ReadTests()
    {
        _services = AppHost.Create();
    }

    private const int MaxCount = 10000;

    [GlobalSetup]
    public async Task Setup()
    {
        await InitializeAsync();
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
        var result = await tx.Commit();

        entityIds = entityIds.Select(e => result[e]).ToList();
        _entityIdsAscending = entityIds.OrderBy(id => id.Value).ToList();
        _entityIdsDescending = entityIds.OrderByDescending(id => id.Value).ToList();

        var idArray = entityIds.ToArray();
        Random.Shared.Shuffle(idArray);
        _entityIdsRandom = idArray.ToList();

        _readId = Ids.Take(Count).Skip(Count / 2).First();

        _db = _connection.Db;
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
        ulong sum = 0;
        sum += _db.Get<File>(_readId).Index;
        return sum;
    }

    public async Task InitializeAsync()
    {
        _connection = await AppHost.CreateConnection(_services);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
