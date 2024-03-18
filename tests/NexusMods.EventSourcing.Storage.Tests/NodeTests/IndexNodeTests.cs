using System.Diagnostics;
using Microsoft.Extensions.Logging;
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
        /*
        Configuration.DataBlockSize = 8;
        Configuration.IndexBlockSize = 8;
*/

        var comparator = Registry.CreateComparator(order);

        var testData = TestData(size).ToArray();

        var grouped = testData
            .GroupBy(d => d.T)
            .OrderBy(g => g.Key)
            .ToArray();

        var index = (IReadable)Nodes.Index.Appendable.Create(comparator);

        var sw = Stopwatch.StartNew();
        var previousSize = 0;
        foreach (var group in grouped)
        {
            var newNode = new Nodes.Data.Appendable { group };
            var sorted = newNode.AsSorted(comparator);

            index = index.Ingest(sorted);


            /*
            if (flush)
                index = (IReadable)index.Pack(NodeStore);
                */


            index.DeepLength.Should().Be(previousSize + newNode.DeepLength, $"because all data should be ingested on group : {group.Key}");
            previousSize += newNode.Length;

        }

        Logger.LogInformation("Ingested {0} datoms in {1} ms", index.DeepLength, sw.ElapsedMilliseconds);

        var allDataNode = (new Nodes.Data.Appendable { testData }).AsSorted(comparator);


        index.DeepLength.Should().Be(testData.Length, "because all data should be ingested");

        var idx = 0;

        sw.Restart();
        foreach (var actual in index)
        {
            var test = allDataNode[idx++];
            actual.Should().Be(test, "because all data should be ingested and returned in the same order");
        }
        Logger.LogInformation("Verified {0} datoms in {1} ms", index.DeepLength, sw.ElapsedMilliseconds);



    }




    public IEnumerable<object[]> IndexTestData()
    {
        foreach (var idx in new[] {SortOrders.EATV, SortOrders.AETV, SortOrders.AVTE})
        {
            foreach (var size in new[] { 1, 2, 3, 4, 8, 16, 128, 1024 * 8, 1024 * 16, 1024 * 128 })
            {
                foreach (var flush in new[] { true, false })
                {
                    yield return [size, idx, flush];
                }
            }
        }
    }
}
