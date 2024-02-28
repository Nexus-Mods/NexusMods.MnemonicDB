using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Iterators;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Sorters;
using Xunit.DependencyInjection;

namespace NexusMods.EventSourcing.Storage.Tests;

public class IndexTests(IServiceProvider provider, IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes, ILogger<IndexTests> logger)
    : AStorageTest(provider, valueSerializers, attributes)
{
    [Theory]
    [MethodData(nameof(TestData))]
    public void CanIngestAndGetDatoms(int entityCount, SortOrders sortOrder, bool flush)
    {
        var comparator = GetComparator(sortOrder);
        var method = GetType().GetMethod(nameof(CanIngestAndGetDatomsInner), BindingFlags.Instance | BindingFlags.NonPublic);
        var generic = method!.MakeGenericMethod(comparator.GetType());
        generic.Invoke(this, [comparator, entityCount, sortOrder, flush]);
    }

    private void CanIngestAndGetDatomsInner<TType>(TType comparator, int entityCount, SortOrders sortOrder, bool flush)
    where TType : IDatomComparator
    {
        var testData = TestDatomChunk(entityCount);

        var index = new AppendableIndexChunk(comparator);


        var grouped = testData
            .GroupBy(d => d.T)
            .ToArray();

        var sw = Stopwatch.StartNew();
        sw.Stop();

        foreach (var group in grouped)
        {
            var newChunk = AppendableChunk.Initialize(group);
            newChunk.Sort(comparator);

            sw.Start();
            index = index.Ingest(newChunk);
            sw.Stop();

        }

        logger.LogInformation("Ingested {DatomCount} datoms in {ElapsedMs}ms", index.Length, sw.ElapsedMilliseconds);

        index.Length.Should().Be(testData.Length, "all datoms should be ingested");

        testData.Sort(comparator);

        for (var i = 0; i < testData.Length; i++)
        {
            AssertEqual(index[i], testData[i], i);
        }
    }


    private IDatomComparator GetComparator(SortOrders sortOrder)
    {
        return sortOrder switch
        {
            SortOrders.EATV => new EATV(_registry),
            SortOrders.AETV => new AETV(_registry),
            _ => throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, null)
        };
    }

    public IEnumerable<object[]> TestData()
    {
        foreach (var idx in new[] {SortOrders.EATV, SortOrders.AETV})
        {
            foreach (var size in new[] { 1, 2, 3, 4, 8, 16, 128, 1024, 1024 * 8, 1024 * 16, 1024 * 128 })
            {
                foreach (var flush in new[] { true,}) // false })
                {
                    yield return [size, idx, flush];
                }
            }
        }
    }

}
