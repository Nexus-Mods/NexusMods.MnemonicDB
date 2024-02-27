using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.Storage.Sorters;
using Xunit.DependencyInjection;

namespace NexusMods.EventSourcing.Storage.Tests;

using DatomLiteral = Tuple<ulong, ushort, ulong, ulong>;

public class ComparatorTests(IServiceProvider provider, IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    : AStorageTest(provider, valueSerializers, attributes)
{

    [Theory]
    [MethodData(nameof(ComparisonTestData))]
    public void EATVTests(Datom a, Datom b, int result)
    {
        var compare = new EATV(_registry);

        compare.Compare(in a, in b).Should().Be(result, "comparison should match expected result");
        compare.Compare(in b, in a).Should().Be(-result, "comparison should be symmetric");
    }


    public IEnumerable<object[]> ComparisonTestData()
    {
        Datom? prev = null;

        var emitters = new Func<EntityId, TxId, ulong, Datom>[]
        {
            (e, tx, v) => _registry.Datom<TestAttributes.FileHash, ulong>(Assert<TestAttributes.FileHash>(e, tx, v)),
            (e, tx, v) => _registry.Datom<TestAttributes.FileName, string>(Assert<TestAttributes.FileName>(e, tx, "file " + v)),
        };

        for (ulong e = 0; e < 2; e++)
        {
            for (var a = 0; a < 2; a ++)
            {
                for (ulong tx = 0; tx < 3; tx++)
                {
                    for (ulong v = 0; v < 3; v++)
                    {


                        var b = emitters[a](EntityId.From(e), TxId.From(tx), v);

                        if (prev != null)
                            yield return [prev, b, -1];
                        prev = b;
                    }

                }
            }
        }
    }
}
