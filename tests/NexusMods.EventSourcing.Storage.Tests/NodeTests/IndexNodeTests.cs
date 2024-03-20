using System.Diagnostics;
using FluentAssertions.Equivalency;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.DatomResults;
using NexusMods.EventSourcing.Storage.Nodes.Data;
using NexusMods.EventSourcing.Storage.Nodes.Index;
using Xunit.DependencyInjection;

namespace NexusMods.EventSourcing.Storage.Tests.NodeTests;

public class IndexNodeTests(IServiceProvider provider) : ADataNodeTests<IndexNodeTests>(provider)
{

    [Theory]
    [MethodData(nameof(IndexTestData))]
    public void CanIngestAndGetDatoms(uint size, SortOrders order)
    {
        var comparator = Registry.CreateComparator(order);

        var root = DataNode.Create();

        var context = new IndexContext
        {
            DataNodeSplitThreshold = 1024,
            IndexNodeSplitThreshold = 1024,
            Store = NodeStore,
            Registry = Registry,
            Comparator = comparator,
            Root = NodeStore.Put(root)
        };

        var testData = TestData(size).ToArray();

        var grouped = testData
            .GroupBy(d => d.T)
            .OrderBy(g => g.Key)
            .ToArray();

        var sw = Stopwatch.StartNew();
        var ingestedSoFar = 0;
        var loop = 0;
        foreach (var group in grouped)
        {
            var newNode = DataNode.Create();
            newNode.Add(group);

            var sorted = newNode.AsSorted(comparator);

            sorted.Length.Should().Be(group.Count(), "because the data should be sorted");

            context.Ingest(sorted);
            ingestedSoFar += group.Count();

            ValidateIndexStructure(context.Root);


            context.All().Length.Should().Be(ingestedSoFar, "because all data should be ingested after loop " + loop);
            loop++;
        }

        var index = context.All();

        Logger.LogInformation("Ingested {0} datoms in {1} ms", index.Length, sw.ElapsedMilliseconds);


        var allDataNode = DataNode.Create();
        allDataNode.Add(testData);
        var sortedAll = allDataNode.AsSorted(comparator);


        index.Length.Should().Be(sortedAll.Length, "because all data should be ingested");

        var idx = 0;

        sw.Restart();
        foreach (var actual in index)
        {
            var test = sortedAll[idx];
            idx++;
            actual.Should().Be(test, "because all data should be ingested and returned in the same order at " + idx);
        }
        Logger.LogInformation("Verified {0} datoms in {1} ms", index.Length, sw.ElapsedMilliseconds);

    }

    private void ValidateIndexStructure(StoreKey contextRoot, int depth = 0)
    {

        depth.Should().BeLessOrEqualTo(2, "because the index should not be too deep");
        var node = NodeStore.Get(contextRoot);
        if (node is not IndexNode indexNode)
        {
            return;
        }

        var totalSize = 0L;
        for (int i = 0; i < indexNode.ChildKeys.Length; i++)
        {
            var child = NodeStore.Get(StoreKey.From(indexNode.ChildKeys[i]));
            if (child is IndexNode indexChild)
            {
                ValidateIndexStructure(StoreKey.From(indexNode.ChildKeys[i]), depth + 1);

                indexNode.ChildCounts[i].Should().Be((ulong)indexChild.DeepLength, "because the child count should be accurate");
                indexNode.ChildOffsets[i].Should().Be((ulong)totalSize, "because the child offset should be accurate");

                if (i < indexNode.ChildKeys.Length - 1)
                {
                    indexNode.GetLastDatom(i).Should().Be(indexChild.GetLastDatom((int)indexChild.ShallowLength - 1), "because the last datom should be accurate");
                }
                indexChild.LastDatom.Should().Be(indexNode.GetLastDatom(i), "because the last datom should be accurate");

                totalSize += indexChild.DeepLength;

            }
            else if (child is DataNode dataChild)
            {
                indexNode.ChildCounts[i].Should().Be((ulong)dataChild.Length, "because the child count should be accurate");
                indexNode.ChildOffsets[i].Should().Be((ulong)totalSize, "because the child offset should be accurate");

                indexNode.GetLastDatom(i).Should().Be(dataChild.All()[(int)(dataChild.Length - 1)], "because the last datom should be accurate");

                totalSize += dataChild.Length;
            }
            else
            {
                throw new InvalidOperationException("Invalid node type in index validation.");
            }
        }

    }


    public IEnumerable<object[]> IndexTestData()
    {
        foreach (var idx in new[] {SortOrders.EATV, SortOrders.AETV, SortOrders.AVTE})
        {
            foreach (var size in new[] { 1, 2, 3, 4, 8, 16, 128})//, 1024 * 8, 1024 * 16, 1024 * 128 })
            {
                yield return [size, idx];
            }
        }
    }
}
