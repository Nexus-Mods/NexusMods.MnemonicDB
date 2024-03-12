using System.Text;
using NexusMods.EventSourcing.Storage.Columns.BlobColumns;
using Xunit.DependencyInjection;

namespace NexusMods.EventSourcing.Storage.Tests.ColumnTests;

public class BlobTests
{

    [Theory]
    [MethodData(nameof(TestData))]
    public void Test(string name, byte[][] values)
    {
        var column = new Appendable();
        foreach (var value in values)
        {
            column.Append(value);
        }

        column.Count.Should().Be(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            column[i].ToArray().Should().Equal(values[i]);
        }

        var packed = ((ICanPack)column).Pack();
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
