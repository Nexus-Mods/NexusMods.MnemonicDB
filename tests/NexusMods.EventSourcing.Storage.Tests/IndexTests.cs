using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Iterators;
using NexusMods.EventSourcing.Storage.Sorters;

namespace NexusMods.EventSourcing.Storage.Tests;

public class IndexTests(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes, ILogger<IndexTests> logger)
    : AStorageTest(valueSerializers, attributes)
{
    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(128)]
    [InlineData(1024)]
    [InlineData(1024 * 8)]
    [InlineData(1024 * 16)]
    [InlineData(1024 * 64)]
    [InlineData(1024 * 128)]
    public void CanIngestAndGetDatoms(int entityCount)
    {
        var grouped = TestDatoms((ulong)entityCount)
            .GroupBy(d => d.TxId)
            .OrderBy(d => d.Key)
            .ToArray();

        var comparator = new Eatv(_registry);
        var index = new Index<Eatv>(comparator, _registry, IndexType.EATV, Configuration.Default);

        var sw = Stopwatch.StartNew();
        foreach (var group in grouped)
        {
            var groupSorted = group.OrderBy(d => d.EntityId)
                .ThenBy(d => d.AttributeId)
                .ThenBy(d => d.TxId)
                .ToArray();
            index.Ingest<ArrayIterator<IRawDatom>, IRawDatom>(groupSorted.Iterate());
            index.Flush(NodeStore);
        }

        logger.LogInformation("Ingested {DatomCount} datoms in {ElapsedMs}ms", index.Count, sw.ElapsedMilliseconds);

        var allSorted = grouped.SelectMany(g => g)
            .OrderBy(d => d.EntityId)
            .ThenBy(d => d.AttributeId)
            .ThenBy(d => d.TxId)
            .ToArray();

        for (var i = 0; i < allSorted.Length; i++)
        {
            var datom = index[i];
            AssertEqual(datom, allSorted[i], i);

        }

        index.Count.Should().Be(grouped.Sum(g => g.Count()), "all datoms should be ingested");

        //index.ChildCount.Should().Be((int)(index.Count / Configuration.Default.IndexBlockSize), "child count should be correct");


    }

}
