using System.Diagnostics;
using Microsoft.Extensions.Logging;
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
        block.Add(allDatoms);

        block.Length.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = allDatoms[i];

            datomA.Should().BeEquivalentTo(datomB);
        }

    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 1)]
    [InlineData(8, 1)]
    [InlineData(13, 7)]
    [InlineData(1024, 16)]
    [InlineData(1024 * 3 - 17, 1024)]
    public void CanSplit(int totalDatoms, int blockSize)
    {
        var block = new Appendable();
        var allDatoms = TestData((uint) totalDatoms).ToArray();
        Random.Shared.Shuffle(allDatoms);

        block.Add(allDatoms);

        var split = block.Split(blockSize).ToArray();

        foreach (var node in split)
        {
            node.Length.Should().BeGreaterThan(blockSize / 2);
            node.Length.Should().BeLessOrEqualTo(blockSize * 2);
        }

        var minSize = split.Min(n => n.Length);
        var maxSize = split.Max(n => n.Length);

        (maxSize - minSize).Should().BeLessThan(2, "nodes should be of similar size");

        var merged = split.SelectMany(n => n);

        var mergedDatoms = merged.ToArray();

        mergedDatoms.Length.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = mergedDatoms[i];
            var datomB = allDatoms[i];
            datomA.Should().BeEquivalentTo(datomB, "datoms should be equal at index " + i);
        }
    }

    [Theory]
    [InlineData(SortOrders.EATV)]
    [InlineData(SortOrders.AETV)]
    [InlineData(SortOrders.AVTE)]
    public void CanSeekToDatom(SortOrders order)
    {
        var compare = Registry.CreateComparator(order);
        var block = new Appendable();
        var allDatoms = TestData(10).ToArray();
        foreach (var datom in allDatoms)
        {
            block.Add(in datom);
        }

        var sorted = allDatoms.Order(CreateComparer(compare)).ToArray();
        var sortedBlock = block.AsSorted(compare);

        for (var i = 0; i < sorted.Length; i++)
        {
            var datom = sorted[i];
            var idx = sortedBlock.Find(in datom, order, Registry);

            idx.Should().Be(i, "both sources are sorted and should match");

            var found = sorted[idx];
            found.Should().BeEquivalentTo(datom, "datoms should be equal at index " + i);
        }
    }


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
    public void CanWriteAndReadBlock(SortOrders order, uint entities)
    {
        var allDatoms = TestData(entities).ToArray();
        var block = new Appendable();
        foreach (var datom in allDatoms)
        {
            block.Add(in datom);
        }

        var sorted = block.AsSorted(Registry.CreateComparator(order));
        var writer = new PooledMemoryBufferWriter();

        var sw = Stopwatch.StartNew();
        sorted.WriteTo(writer);
        Logger.LogInformation("Packed {0} datoms into {1} bytes in {2}ms", block.Length, writer.WrittenMemory.Length, sw.ElapsedMilliseconds);

        sw.Restart();
        var readNode = ExtensionMethods.ReadDataNode(writer.WrittenMemory);
        Logger.LogInformation("Read {0} datoms from {1} bytes in {2}ms", readNode.Length, writer.WrittenMemory.Length, sw.ElapsedMilliseconds);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = readNode[i];
            var datomB = sorted[i];

            datomA.Should().BeEquivalentTo(datomB, "datoms should be equal at index " + i);
        }
    }
}
