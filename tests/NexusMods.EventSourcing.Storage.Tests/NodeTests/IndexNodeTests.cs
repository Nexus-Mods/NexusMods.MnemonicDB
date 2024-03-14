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
    public void CanIngestAndGetDatoms(uint size, SortOrders order, bool flush)
    {
        Configuration.DataBlockSize = 8;
        Configuration.IndexBlockSize = 8;


        var comparator = Registry.CreateComparator(order);

        var testData = TestData(size).ToArray();

        var grouped = testData
            .GroupBy(d => d.T)
            .OrderBy(g => g.Key)
            .ToArray();

        var index = (IAppendable)Nodes.Index.Appendable.Create(comparator);

        var previousSize = 0;
        foreach (var group in grouped)
        {
            var newNode = new Nodes.Data.Appendable { group };
            var sorted = newNode.AsSorted(comparator);

            index = index.Ingest(sorted);
            index.DeepLength.Should().Be(previousSize + newNode.DeepLength, $"because all data should be ingested on group : {group.Key}");
            previousSize += newNode.Length;
        }

        var allDataNode = (new Nodes.Data.Appendable { testData }).AsSorted(comparator);


        index.DeepLength.Should().Be(testData.Length, "because all data should be ingested");

        for (var idx = 0; idx < testData.Length; idx++)
        {
            var test = allDataNode[idx];
            var actual = index[idx];

            actual.Should().Be(test);
        }

    }




    public IEnumerable<object[]> IndexTestData()
    {
        foreach (var idx in new[] {SortOrders.EATV, SortOrders.AETV, SortOrders.AVTE})
        {
            foreach (var size in new[] { //1,
                         2, })
                         //3, 4, 8, 16, 128, 1024, 1024 * 8})//, 1024 * 16, 1024 * 128 })
            {
                foreach (var flush in new[] { true })//, false })
                {
                    yield return [size, idx, flush];
                }
            }
        }
    }
}
