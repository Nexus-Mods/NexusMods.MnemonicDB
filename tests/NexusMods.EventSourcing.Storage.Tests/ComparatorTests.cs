using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.Storage.Sorters;
using Xunit.DependencyInjection;

namespace NexusMods.EventSourcing.Storage.Tests;

using DatomLiteral = Tuple<ulong, ushort, ulong, ulong>;

public class ComparatorTests(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    : AStorageTest(valueSerializers, attributes)
{

    [Theory]
    [MethodData(nameof(ComparisonTestData))]
    public void EATVTests(IRawDatom a, IRawDatom b, int result)
    {
        var compare = new Eatv(_registry);
        compare.Compare(in a, in b).Should().Be(result, "comparison should match expected result");
        compare.Compare(in b, in a).Should().Be(-result, "comparison should be symmetric");
    }


    public IEnumerable<object[]> ComparisonTestData()
    {
        IRawDatom? prev = null;

        var emitters = new Func<ulong, ulong, ulong, IRawDatom>[]
        {
            (e, tx, v) => Assert<TestAttributes.FileHash>(e, tx, v),
            (e, tx, v) => Assert<TestAttributes.FileName>(e, tx, "file " + v),
        };

        for (ulong e = 0; e < 2; e++)
        {
            for (var a = 0; a < 2; a ++)
            {
                for (ulong tx = 0; tx < 3; tx++)
                {
                    for (ulong v = 0; v < 3; v++)
                    {


                        var b = emitters[a](e, tx, v);

                        if (prev != null)
                            yield return [prev, b, -1];
                        prev = b;
                    }

                }
            }
        }
    }
}
