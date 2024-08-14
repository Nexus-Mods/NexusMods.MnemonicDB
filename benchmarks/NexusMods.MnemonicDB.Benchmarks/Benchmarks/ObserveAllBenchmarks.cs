using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using NexusMods.MnemonicDB.TestModel;

namespace NexusMods.MnemonicDB.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ObserveAllBenchmarks : ABenchmark
{
    private string[] _names = [];

    [Params(10, 100, 1_000, 10_000)]
    public int N { get; set; }

    [GlobalSetup]
    public async ValueTask GlobalSetup()
    {
        await InitializeAsync();
        _names = Enumerable.Range(start: 0, count: N).Select(i => $"Loadout {i}").ToArray();
    }

    [Benchmark]
    public async ValueTask ObserveAll()
    {
        using var disposable = Loadout.ObserveAll(Connection).Subscribe(_ => { });

        using var tx = Connection.BeginTransaction();

        foreach (var name in _names)
        {
            _ = new Loadout.New(tx)
            {
                Name = name,
            };
        }

        await tx.Commit();
    }
}
