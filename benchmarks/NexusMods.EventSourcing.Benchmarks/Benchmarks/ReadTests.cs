using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Benchmarks.Model;

namespace NexusMods.EventSourcing.Benchmarks.Benchmarks;

public class ReadTests
{
    private readonly IConnection _connection;
    private readonly List<EntityId> _entityIds;

    public ReadTests()
    {
        var services = AppHost.Create();

        _connection = services.GetRequiredService<IConnection>();
        _entityIds = new List<EntityId>();
    }

    [GlobalSetup]
    public void Setup()
    {
        var tx = _connection.BeginTransaction();
        _entityIds.Clear();
        for (var i = 0; i < Count; i++)
        {
            var id = Ids.MakeId(Ids.Partition.Entity, (ulong)i);
            File.Hash.Assert(tx.TempId(), (ulong)i, tx);
            File.Path.Assert(tx.TempId(), $"C:\\test_{i}.txt", tx);
            File.Index.Assert(tx.TempId(), (ulong)i, tx);
            _entityIds.Add(EntityId.From(id));
        }
        tx.Commit();
    }


    [Params(1, 10, 100, 1000)]
    public int Count { get; set; } = 1000;

    [Benchmark]
    public int ReadFiles()
    {
        var db = _connection.Db;
        var read = db.Get<Model.FileReadModel>(_entityIds).ToList();
        return read.Count;
    }

}
