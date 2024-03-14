using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Nodes.Index;
using NexusMods.EventSourcing.Storage.Nodes.Data;
using NexusMods.EventSourcing.Storage.Nodes.Index;
using Xunit.DependencyInjection;

namespace NexusMods.EventSourcing.Storage.Tests.NodeTests;

public class IndexNodeTests(IServiceProvider provider) : ADataNodeTests<IndexNodeTests>(provider)
{

    [Theory]
    [MethodData(nameof(IndexTestData))]
    public void CanIngestAndGetDatoms(uint size, SortOrders idx, bool flush)
    {
        var comparator = Registry.CreateComparator(idx);

        var testData = TestData(size).ToArray();

        var grouped = testData
            .GroupBy(d => d.T)
            .OrderBy(g => g.Key)
            .ToArray();

        var index = (IAppendable)new Nodes.Index.Appendable(comparator);

        foreach (var group in grouped)
        {
            var newNode = new Nodes.Data.Appendable { group };
            var sorted = newNode.AsSorted(comparator);

            index = index.Ingest(sorted);
        }

        index.Length.Should().Be(testData.Length, "because all data should be ingested");

    }




    public IEnumerable<object[]> IndexTestData()
    {
        foreach (var idx in new[] {SortOrders.EATV, SortOrders.AETV, SortOrders.AVTE})
        {
            foreach (var size in new[] { 1, 2, 3, 4, 8, 16, 128, 1024, 1024 * 8, 1024 * 16, 1024 * 128 })
            {
                foreach (var flush in new[] { true, false })
                {
                    yield return [size, idx, flush];
                }
            }
        }
    }
}
