using NexusMods.EventSourcing.Storage.Columns;

namespace NexusMods.EventSourcing.Storage.Tests.ColumnTests;

public class AppendableBlobColumnTests
{
    [Fact]
    public void CanReadAndWriteInts()
    {
        var column = new AppendableBlobColumn();
        for (ulong i = 0; i < 100; i++)
        {
            column.Append(BitConverter.GetBytes(i));
        }

        for (ulong i = 0; i < 100; i++)
        {
            BitConverter.ToUInt64(column[(int)i].Span).Should().Be(i);
        }
    }

    [Fact]
    public void CanPack()
    {
        var column = new AppendableBlobColumn();
        for (ulong i = 0; i < 1024; i++)
        {
            column.Append(BitConverter.GetBytes(i));
        }

        var packed = column.Pack();

        packed.Length.Should().Be(1024);

        for (ulong i = 0; i < 1024; i++)
        {
            BitConverter.ToUInt64(packed[(int)i].Span).Should().Be(i);
        }
    }
}
