using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Sorters;
using Xunit.DependencyInjection;

namespace NexusMods.EventSourcing.Storage.Tests;


public class ComparatorTests(IServiceProvider provider) : AStorageTest(provider)
{

    [Theory]
    [MethodData(nameof(ComparisonTestData))]
    public void EATVTests(Datom a, Datom b, int result)
    {
        var compare = new EATV(_registry);

        compare.Compare(in a, in b).Should().NotBe(0);
        compare.Compare(in a, in b).Should().Be(-compare.Compare(in b, in a), "comparison should be symmetric");
    }


    public IEnumerable<object[]> ComparisonTestData()
    {
        var chunk = new AppendableChunk();

        var emitters = new Action<EntityId, TxId, ulong>[]
        {
            (e, tx, v) => _registry.Append<TestAttributes.FileHash, ulong>(chunk, e, tx, DatomFlags.Added, v),
            (e, tx, v) => _registry.Append<TestAttributes.FileName, string>(chunk, e, tx, DatomFlags.Added, "file " + v),
        };

        for (ulong e = 0; e < 2; e++)
        {
            for (var a = 0; a < 2; a ++)
            {
                for (ulong tx = 0; tx < 3; tx++)
                {
                    for (ulong v = 0; v < 3; v++)
                    {
                        emitters[a](EntityId.From(e), TxId.From(tx), v);
                    }
                }
            }
        }

        Datom? prev = null;

        foreach (var datom in chunk)
        {
            if (prev != null)
            {
                yield return [prev, datom, 1];
            }
            prev = datom;
        }
    }
}
