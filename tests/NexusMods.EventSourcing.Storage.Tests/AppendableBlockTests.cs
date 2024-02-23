using System.Collections;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Sorters;

namespace NexusMods.EventSourcing.Storage.Tests;

public class AppendableBlockTests(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    : AStorageTest(valueSerializers, attributes)
{
    [Fact]
    public void CanAppendDataToBlock()
    {
        var block = new AppendableBlock(Configuration.Default);
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

    [Fact]
    public void CanSortBlock()
    {
        var block = new AppendableBlock(Configuration.Default);
        var allDatoms = TestData(10).ToArray();
        Random.Shared.Shuffle(allDatoms);

        foreach (var datom in TestData(10))
        {
            block.Append(in datom);
        }

        block.Sort(new Eatv(_registry));

        var sorted = allDatoms.OrderBy(d => d.EntityId)
            .ThenBy(d => d.AttributeId)
            .ThenBy(d => d.TxId)
            .ThenBy(d => d.ValueLiteral)
            .ToArray();

        block.Count.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = sorted[i];
            AssertEqual(datomA, datomB, i);
        }
    }

    [Fact]
    public void InsertingMaintainsOrder()
    {
        var datoms = TestData(10).ToArray();

        var insertBlock = new AppendableBlock(Configuration.Default);
        var compare = new Eatv(_registry);

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

    [Fact]
    public void CanReadAndWriteBlocks()
    {
        var allDatoms = TestData(10).ToArray();
        var block = new AppendableBlock(Configuration.Default);
        foreach (var datom in TestData(10))
        {
            block.Append(in datom);
        }

        var writer = new PooledMemoryBufferWriter();
        block.WriteTo(writer);

        var block2 = new AppendableBlock(Configuration.Default);
        block2.InitializeFrom(writer.GetWrittenSpan());

        block2.Count.Should().Be(allDatoms.Length);

        for (var i = 0; i < allDatoms.Length; i++)
        {
            var datomA = block[i];
            var datomB = allDatoms[i];

            AssertEqual(datomA, datomB, i);
        }
    }

    [Fact]
    public void CanSeekToDatom()
    {
        var compare = new Eatv(_registry);
        var block = new AppendableBlock(Configuration.Default);
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
