using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Benchmarks.Benchmarks;

public class WriteTests
{
    /*
    private readonly IConnection _connection;

    public WriteTests()
    {
        var services = AppHost.Create();

        _connection = services.GetRequiredService<IConnection>();
    }


    [Params(1, 10, 100, 1000)]
    public int Count { get; set; } = 1000;

    [Benchmark]
    public async Task AddFiles()
    {
        var tx = _connection.BeginTransaction();
        var ids = new List<EntityId>();
        for (var i = 0; i < Count; i++)
        {
            var file = new File(tx)
            {
                Hash = (ulong)i,
                Path = $"C:\\test_{i}.txt",
                Index = (ulong)i
            };
            ids.Add(file.Id);
        }
        var result = await tx.Commit();

        ids = ids.Select(id => result[id]).ToList();

        var db = _connection.Db;
        foreach (var id in ids)
        {
            var loaded = db.Get<File>(id);

            loaded.Should().NotBeNull("the entity should be in the database");
        }
    }
    */

}
