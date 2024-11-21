using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine;
using NexusMods.MnemonicDB.TestModel;
using static NexusMods.MnemonicDB.QueryEngine.QueryPredicates;

namespace NexusMods.MnemonicDB.Tests;

public class BasicQueryTests
{
    [Fact]
    public void CanUnpivot()
    {
        LVar<EntityId> e = LVar.Create<EntityId>();
        LVar<string> name = LVar.Create<string>();
        var q = new Query<IEnumerable<int>, IEnumerable<int>>(out var a, out var b)
        {
            Unpivot(a, out var dest1),
            Unpivot(b, out var dest2),
            {e, Loadout.Name, name},
            Unify(dest1, dest2),
        }.Return(dest1);

        var input1 = new[] { 1, 2, 3 };
        var input2 = new[] { 2, 3, 4 };
        q.Run(input1, input2).Should().BeEquivalentTo([1, 2, 3]);

    }
    
}
