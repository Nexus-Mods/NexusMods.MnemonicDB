using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes.Data;

namespace NexusMods.EventSourcing.Storage.Tests.NodeTests;

public class SortedNodeTests(IServiceProvider provider) : ADataNodeTests<SortedNodeTests>(provider)
{
    [Theory]
    [InlineData(SortOrders.EATV, 1)]
    [InlineData(SortOrders.EATV, 2)]
    [InlineData(SortOrders.EATV, 4)]
    [InlineData(SortOrders.EATV, 8)]
    [InlineData(SortOrders.EATV, 16)]
    [InlineData(SortOrders.EATV, 1024)]
    [InlineData(SortOrders.EATV, 1024 * 16)]
    [InlineData(SortOrders.AETV, 1)]
    [InlineData(SortOrders.AETV, 2)]
    [InlineData(SortOrders.AETV, 4)]
    [InlineData(SortOrders.AETV, 8)]
    [InlineData(SortOrders.AETV, 16)]
    [InlineData(SortOrders.AETV, 1024)]
    [InlineData(SortOrders.AETV, 1024 * 16)]
    [InlineData(SortOrders.AVTE, 1)]
    [InlineData(SortOrders.AVTE, 2)]
    [InlineData(SortOrders.AVTE, 4)]
    [InlineData(SortOrders.AVTE, 8)]
    [InlineData(SortOrders.AVTE, 16)]
    [InlineData(SortOrders.AVTE, 1024)]
    [InlineData(SortOrders.AVTE, 1024 * 16)]
    public void CanSortBlock(SortOrders order, uint entities)
    {
        var block = new Appendable();
        var allDatoms = TestData(entities).ToArray();
        Random.Shared.Shuffle(allDatoms);

        foreach (var datom in TestData(entities))
        {
            block.Add(in datom);
        }

        var compare = Registry.CreateComparator(order);
        Logger.LogInformation("Sorting {0} datoms", allDatoms.Length);
        block.Sort(compare);

        var sorted = allDatoms.Order(CreateComparer(compare))
            .ToArray();

        block.Length.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = sorted[i];
            datomA.Should().BeEquivalentTo(datomB);
        }
    }
}
