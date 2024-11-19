using DynamicData.Kernel;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.TestModel;
using static NexusMods.MnemonicDB.Abstractions.Query.QueryPredicates;

namespace NexusMods.MnemonicDB.Tests;

public partial class BasicQueryTests
{
    [Fact]
    public void CanUnpivot()
    {
        var q = new Query<IEnumerable<int>, IEnumerable<int>>(out var a, out var b)
        {
            Unpivot<int>(a, out var dest),
        }.Return(dest);

        var input = new[] { 1, 2, 3 };
        q.Run(input).Should().BeEquivalentTo([1, 2, 3]);

    }
    
}
