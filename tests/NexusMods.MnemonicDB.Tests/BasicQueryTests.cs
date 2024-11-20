using NexusMods.MnemonicDB.QueryEngine;
using static NexusMods.MnemonicDB.QueryEngine.QueryPredicates;

namespace NexusMods.MnemonicDB.Tests;

public class BasicQueryTests
{
    [Fact]
    public void CanUnpivot()
    {
        var q = new Query<IEnumerable<int>, IEnumerable<int>>(out var a, out var b)
        {
            Unpivot(a, out var dest),
            Unpivot(a, out var dest2),
            ProjectTuple(dest, dest2, out var final)
        }.Return(final);

        var input = new[] { 1, 2, 3 };
        q.Run(input).Should().BeEquivalentTo([1, 2, 3]);

    }
    
}
