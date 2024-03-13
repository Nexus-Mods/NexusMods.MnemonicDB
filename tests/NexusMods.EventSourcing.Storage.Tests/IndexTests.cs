using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Nodes.IndexNode;
using Xunit.DependencyInjection;

namespace NexusMods.EventSourcing.Storage.Tests;

public class IndexTests(IServiceProvider provider) : AStorageTest(provider)
{
    [Theory]
    [MethodData(nameof(TestData))]
    public void CanIngestAndGetDatoms(int entityCount, SortOrders sortOrder, bool flush)
    {
        var comparator = _registry.CreateComparator(sortOrder);
        var method = GetType().GetMethod(nameof(CanIngestAndGetDatomsInner), BindingFlags.Instance | BindingFlags.NonPublic);
        var generic = method!.MakeGenericMethod(comparator.GetType());
        generic.Invoke(this, [comparator, entityCount, sortOrder, flush]);
    }

    private void CanIngestAndGetDatomsInner<TType>(TType comparator, int entityCount, SortOrders sortOrder, bool flush)
    where TType : IDatomComparator
    {
        var testData = TestDatomNode(entityCount);

        var index = new AppendableIndexNode(comparator);


        var grouped = testData
            .GroupBy(d => d.T)
            .ToArray();

        var sw = Stopwatch.StartNew();
        sw.Stop();

        foreach (var group in grouped)
        {
            var newNode = AppendableNode.Initialize(group);
            newNode.Sort(comparator);

            sw.Start();
            index = index.Ingest(newNode);
            sw.Stop();

            if (flush)
                index = AppendableIndexNode.UnpackFrom((IIndexNode)index.Flush(NodeStore));

        }


        var finalIndex = (IIndexNode)index.Flush(NodeStore);
        Logger.LogInformation("Ingested {DatomCount} datoms in {ElapsedMs}ms", finalIndex.Length, sw.ElapsedMilliseconds);

        finalIndex.Length.Should().Be(testData.Length, "all datoms should be ingested");

        testData.Sort(comparator);

        for (var i = 0; i < testData.Length; i++)
        {
            var testDatom = testData[i];

            var idx = finalIndex.Find(0, finalIndex.Length, testDatom, sortOrder, _registry);
            idx.Should().Be(i, "the index should find the correct datom");
            AssertEqual(finalIndex[i], testData[i], i);
        }
    }

    public IEnumerable<object[]> TestData()
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
