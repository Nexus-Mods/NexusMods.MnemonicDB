using System.Buffers;
using System.Runtime.InteropServices;
using FlatSharp;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Columns.ULongColumns;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;
using Reloaded.Memory.Extensions;
using Xunit.DependencyInjection;

namespace NexusMods.EventSourcing.Storage.Tests.ColumnTests;

public class ULongColumnTests
{
    [Fact]
    public void MultipleValuesColumnsPackIntoMinMax()
    {
        var column = (IUnpacked)Create(42, 43, 44);
        var packed = column.Pack();

    }

    [Theory]
    [MethodData(nameof(TestData))]
    public Task CanGetColumnStatistics(string comment, ulong[] values)
    {
        var column = (IUnpacked)Create(values);
        var stats = Statistics.Create(MemoryMarshal.Cast<ulong, ulong>(column.Span));
        return Verify(stats).UseTextForParameters(comment);
    }

    [Theory]
    [MethodData(nameof(TestData))]
    public void PackedDataShouldRoundTrip(string name, ulong[] values)
    {
        var column = (IUnpacked)Create(values);
        var packed = column.Pack();
        AssertEqual(packed, (IReadable)column);

        var unpacked = packed.Unpack();
        AssertEqual(unpacked, (IReadable)column);

        var writer = new ArrayBufferWriter<byte>();
        ULongPackedColumn.Serializer.Write(writer, (ULongPackedColumn)packed);
        var unpackedUL = ULongPackedColumn.Serializer.Parse(writer.WrittenMemory);
        AssertEqual(unpackedUL, (IReadable)column);


    }

    [Theory]
    [MethodData(nameof(TestData))]
    public Task PackedDataShouldHaveCorrectHeaders(string name, ulong[] data)
    {
        var column = (IUnpacked)Create(data);
        var packed = column.Pack();

        var casted = (ULongPackedColumn)packed;
        var values = new List<ulong>();

        if (casted.Header.Kind != UL_Column_Union.ItemKind.Packed)
            return Task.CompletedTask;

        casted.Header.Kind.Should().Be(UL_Column_Union.ItemKind.Packed, "the column should be packed");

        var packedHeader = casted.Header.Packed;

        var mask = (1UL << (packedHeader.ValueBytes * 8)) - 1;
        var dataSpan = casted.Data.Span;

        for (var i = 0; i < casted.Length; i++)
        {
            values.Add(MemoryMarshal.Read<ulong>(dataSpan.SliceFast(i * packedHeader.ValueBytes)) & mask);
        }

        return Verify(new
            {
                Header = casted.Header.Packed,
                Values = values.ToArray()
            }).UseTextForParameters(name);
    }


    private Appendable Create(params ulong[] values)
    {
        var column = Appendable.Create(values.Length);
        column.Append(values.AsSpan());
        return column;
    }

    private IEnumerable<object[]> TestData()
    {
        var data = new (string Name, ulong[] Values)[]
        {
            ("Empty", []),
            ("One value", [42]),
            ("One value two entries", [42, 42]),
            ("Two values", [42, 43]),
            ("Zero in two partitions", [Ids.MakeId(Ids.Partition.Attribute, 0), Ids.MakeId(Ids.Partition.Tx, 0)]),
            ("Same value two partitions", [Ids.MakeId(Ids.Partition.Attribute, 1), Ids.MakeId(Ids.Partition.Entity, 1)]),
            ("Deoptimization case, two large numbers", [ulong.MaxValue, ulong.MinValue, ulong.MinValue + 1, ulong.MaxValue - 1])
        };

        foreach (var (name, values) in data)
        {
            yield return [name, values];
        }

        foreach (var bytes in (int[])[1, 2, 3, 4, 5, 6, 7])
        {
            yield return [bytes + " byte values", new ulong[] { 0, 1UL << (8 * bytes) - 1}];
        }
    }

    private void AssertEqual(IReadable a, IReadable b)
    {
        a.Length.Should().Be(b.Length, "the columns should have the same length.");
        for (var i = 0; i < a.Length; i++)
        {
            a[i].Should().Be(b[i], "the columns should have the same values at index " + i);
        }
    }
}
