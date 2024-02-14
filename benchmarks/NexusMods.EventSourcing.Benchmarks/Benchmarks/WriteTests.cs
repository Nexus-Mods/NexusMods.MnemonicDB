using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Benchmarks.Model;

namespace NexusMods.EventSourcing.Benchmarks.Benchmarks;

public class WriteTests
{
    private readonly IConnection _connection;

    public WriteTests()
    {
        var services = AppHost.Create();

        _connection = services.GetRequiredService<IConnection>();
    }


    [Params(1, 10, 100, 1000)]
    public int Count { get; set; } = 1000;

    [Benchmark]
    public void AddFiles()
    {
        var tx = _connection.BeginTransaction();
        for (var i = 0; i < Count; i++)
        {
            var id = Ids.MakeId(Ids.Partition.Entity, (ulong)i);
            File.Hash.Assert(tx.TempId(), (ulong)i, tx);
            File.Path.Assert(tx.TempId(), $"C:\\test_{i}.txt", tx);
            File.Index.Assert(tx.TempId(), (ulong)i, tx);
        }
        tx.Commit();
    }

}
