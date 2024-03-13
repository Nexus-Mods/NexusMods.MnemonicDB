using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Tests.NodeTests;

public class DataTests(IServiceProvider provider) : ADataNodeTests<DataTests>(provider)
{

    [Fact]
    public void CanAppendDataToBlock()
    {
        var block = new Appendable();
        var allDatoms = TestData(10).ToArray();
        foreach (var datom in TestData(10))
        {
            block.Add(datom);
        }

        block.Length.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = allDatoms[i];

            datomA.Should().BeEquivalentTo(datomB);
        }

    }




}
