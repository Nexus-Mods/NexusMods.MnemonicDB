using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Iterators;
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

        var grouped = GenerateData(entityCount, comparator);

        var index = new Index<TType>(comparator, _registry, SortType.EATV, Configuration.Default);

        var sw = Stopwatch.StartNew();
        foreach (var group in grouped)
        {
            var groupSorted = group.OrderBy(d => d.EntityId)
                .ThenBy(d => d.AttributeId)
                .ThenBy(d => d.TxId)
                .ToArray();
            index.Ingest<ArrayIterator<IRawDatom>, IRawDatom>(groupSorted.Iterate());

            if (flush)
                index.Flush(NodeStore);
        }

        logger.LogInformation("Ingested {DatomCount} datoms in {ElapsedMs}ms", index.Count, sw.ElapsedMilliseconds);


        var allSorted = SortTestData(sortOrder, grouped);

        for (var i = 0; i < allSorted.Length; i++)
        {
            var datom = index[i];
            AssertEqual(datom, allSorted[i], i);

        }

        index.Count.Should().Be(grouped.Sum(g => g.Count()), "all datoms should be ingested");

        //index.ChildCount.Should().Be((int)(index.Count / Configuration.Default.IndexBlockSize), "child count should be correct");
    }

    private IGrouping<ulong, IRawDatom>[] GenerateData<TComparator>(int entityCount, TComparator comparator) where TComparator : IDatomComparator
    {
        var grouped = TestDatoms((ulong)entityCount)
            .Order(Comparer<IRawDatom>.Create((a, b) => comparator.Compare(a, b)))
            .GroupBy(d => d.TxId)
            .OrderBy(d => d.Key)
            .ToArray();
        return grouped;
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
    private IRawDatom[] SortTestData(SortOrders sortOrder, IGrouping<ulong, IRawDatom>[] grouped)
    {
        var comparator = GetComparator(sortOrder);
        var allSorted = grouped.SelectMany(g => g)
            .Order(Comparer<IRawDatom>.Create((a, b) => comparator.Compare(a, b)))
            .ToArray();
        return allSorted;
    }

    public IEnumerable<object[]> TestData()
    {
        foreach (var idx in new[] {SortOrders.EATV, SortOrders.AETV})
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
