using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Abstractions.Columns.PackedColumns;

namespace NexusMods.EventSourcing.Storage.Tests.ColumnTests;

public class ManagedAppendableColumnTests
{
    [Theory]
    [InlineData(0, 100, typeof(UnsignedOffsetPackedColumn<ulong, byte>))]
    [InlineData(ushort.MaxValue, 100, typeof(UnsignedOffsetPackedColumn<ulong, byte>))]
    [InlineData(ushort.MaxValue, ushort.MaxValue, typeof(UnsignedOffsetPackedColumn<ulong, ushort>))]

    public void CanAddItems(ulong start, ulong length, Type type)
    {
        var column = new ManagedAppendableColumn<ulong>();

        var items = Enumerable.Range((int)start, (int)length)
            .Select(i => (ulong)i)
            .ToArray();

        foreach (var item in items)
            column.Append(item);

        column.Length.Should().Be(items.Length);

        for (var i = 0; i < items.Length; i++)
            column[i].Should().Be(items[i]);

        var packed = column.Pack();

        packed.Length.Should().Be(items.Length);
        packed.Should().BeOfType(type);

        for (var i = 0; i < items.Length; i++)
            packed[i].Should().Be(items[i]);

    }
}
