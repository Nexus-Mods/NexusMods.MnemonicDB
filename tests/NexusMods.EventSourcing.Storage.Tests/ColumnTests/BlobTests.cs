using System.Buffers;
using System.Text;
using FlatSharp;
using NexusMods.EventSourcing.Storage.Columns.BlobColumns;
using NexusMods.Hashing.xxHash64;
using Xunit.DependencyInjection;
using IUnpacked = NexusMods.EventSourcing.Abstractions.Columns.BlobColumns.IUnpacked;

namespace NexusMods.EventSourcing.Storage.Tests.ColumnTests;

public class BlobTests
{

    [Theory]
    [MethodData(nameof(TestData))]
    public Task Test(string name, byte[][] values)
    {
        var column = Appendable.Create();
        foreach (var value in values)
        {
            column.Append(value);
        }

        column.Count.Should().Be(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            column[i].ToArray().Should().Equal(values[i]);
        }

        var packed = ((IUnpacked)column).Pack();

        packed.Count.Should().Be(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            packed[i].ToArray().Should().Equal(values[i]);
        }


        var writer = new ArrayBufferWriter<byte>();
        BlobPackedColumn.Serializer.Write(writer, (BlobPackedColumn)packed);

        var unpacked = BlobPackedColumn.Serializer.Parse(writer.WrittenMemory);
        unpacked.Count.Should().Be(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            unpacked[i].ToArray().Should().Equal(values[i]);
        }

        return Verify(
        new {
            Hash = writer.WrittenSpan.XxHash64().ToHex(),
            Size = writer.WrittenSpan.Length,
        })
            .UseTextForParameters(name);
    }


    public IEnumerable<object[]> TestData()
    {
        var data = new (string, object[])[]
        {
            ("Empty", []),
            ("One Integer", [1]),
            ("One string", ["test"]),
            ("Two Integers", [1, 2]),
            ("Two Strings", ["test", "test2"]),
            ("Mixed", [1, "test", 2, "test2"]),
        };

        byte[] ConvertValue(object value)
        {
            if (value is int i)
            {
                return BitConverter.GetBytes(i);
            }
            if (value is string s)
            {
                return Encoding.UTF8.GetBytes(s);
            }
            throw new NotSupportedException("Unsupported type " + value.GetType());
        }

        foreach (var (name, values) in data)
        {
            yield return [name, values.Select(ConvertValue).ToArray()];
        }

    }
}
