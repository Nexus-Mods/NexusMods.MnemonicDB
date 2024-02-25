using System.Collections;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Sorters;

namespace NexusMods.EventSourcing.Storage.Tests;

public class AppendableNodeTests(IServiceProvider provider, IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    : AStorageTest(provider, valueSerializers, attributes)
{
    [Fact]
    public void CanAppendDataToBlock()
    {
        var block = new AppendableNode(Configuration.Default);
        var allDatoms = TestData(10).ToArray();
        foreach (var datom in TestData(10))
        {
            block.Append(in datom);
        }

        block.Count.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = allDatoms[i];

            AssertEqual(datomA, datomB, i);
        }
    }

    [Theory]
    [InlineData(SortOrders.EATV)]
    [InlineData(SortOrders.AETV)]
    public void CanMergeBlock(SortOrders orders)
    {
        var block = new AppendableNode(Configuration.Default);
        var allDatoms = TestData(10).ToArray();

        Random.Shared.Shuffle(allDatoms);

        var block2 = new AppendableNode(Configuration.Default);

        var half = allDatoms.Length / 2;
        for (var i = 0; i < half; i++)
        {
            block.Append(in allDatoms[i]);
        }

        for (var i = half; i < allDatoms.Length; i++)
        {
            block2.Append(in allDatoms[i]);
        }

        var compare = IDatomComparator.Create(orders, _registry);
        block.Sort(compare);
        block2.Sort(compare);

        block.Ingest<AppendableNode.FlyweightIterator, AppendableNode.FlyweightRawDatom, OnHeapDatom, IDatomComparator>(block2.Iterate(), OnHeapDatom.Max, compare);

        block.Count.Should().Be(allDatoms.Length);

        var sorted = allDatoms.Order(CreateComparer(compare))
            .ToArray();

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = sorted[i];
            AssertEqual(datomA, datomB, i);
        }
    }

    [Theory]
    [InlineData(SortOrders.EATV)]
    [InlineData(SortOrders.AETV)]
    public void CanSortBlock(SortOrders order)
    {
        var block = new AppendableNode(Configuration.Default);
        var allDatoms = TestData(10).ToArray();
        Random.Shared.Shuffle(allDatoms);

        foreach (var datom in TestData(10))
        {
            block.Append(in datom);
        }

        var compare = IDatomComparator.Create(order, _registry);
        block.Sort(compare);

        var sorted = allDatoms.Order(CreateComparer(compare))
            .ToArray();

        block.Count.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = sorted[i];
            AssertEqual(datomA, datomB, i);
        }
    }

    [Theory]
    [InlineData(SortOrders.EATV)]
    [InlineData(SortOrders.AETV)]
    public void InsertingMaintainsOrder(SortOrders order)
    {
        var compare = IDatomComparator.Create(order, _registry);
        var datoms = TestData(10)
            .ToArray();

        var insertBlock = new AppendableNode(Configuration.Default);

        for (var i = 0; i < datoms.Length; i++)
        {
            insertBlock.Insert(in datoms[i], compare);
        }

        var sorted = datoms.Order(CreateComparer(compare))
            .ToArray();

        insertBlock.Count.Should().Be(datoms.Length);

        for (var i = 0; i < datoms.Length; i++)
        {
            var datomA = insertBlock[i];
            var datomB = sorted[i];

            AssertEqual(datomA, datomB, i);
        }
    }

    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(128)]
    [InlineData(1024)]
    [InlineData(1024 * 8)]
    public void CanReadAndWriteBlocks(uint count)
    {
        var allDatoms = TestData(count).ToArray();
        var block = new AppendableNode(Configuration.Default);
        foreach (var datom in allDatoms)
        {
            block.Append(in datom);
        }

        var writer = new PooledMemoryBufferWriter();
        block.WriteTo(writer);

        var block2 = new AppendableNode(Configuration.Default);
        block2.InitializeFrom(writer.GetWrittenSpan());

        block2.Count.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block2[i];
            var datomB = allDatoms[i];

            AssertEqual(datomA, datomB, i);
        }
    }

    [Fact]
    public void CanSeekToDatom()
    {
        var compare = new EATV(_registry);
        var block = new AppendableNode(Configuration.Default);
        var allDatoms = TestData(10).ToArray();
        foreach (var datom in allDatoms)
        {
            block.Append(in datom);
        }

        var sorted = allDatoms.Order(CreateComparer(compare)).ToArray();
        block.Sort(compare);

        for (var i = 0; i < sorted.Length; i++)
        {
            var datom = sorted[i];
            var iter = block.Seek(datom, compare);

            iter.Index.Should().Be(i, "for index " + i);

            iter.Value(out var current);
            AssertEqual(current, datom, i);
        }
    }

    public IComparer<IRawDatom> CreateComparer(IDatomComparator datomComparator)
    {
        return Comparer<IRawDatom>.Create((a, b) => datomComparator.Compare(in a, in b));
    }

    public IEnumerable<IRawDatom> TestData(uint max)
    {
        for (ulong eid = 0; eid < max; eid += 1)
        {
            for (ulong tx = 0; tx < 10; tx += 1)
            {
                for (ulong val = 0; val < 10; val += 1)
                {
                    yield return Assert<TestAttributes.FileHash>(eid, tx, val);
                    yield return Assert<TestAttributes.FileName>(eid, tx, " file " + val);
                }
            }
        }
    }
}
