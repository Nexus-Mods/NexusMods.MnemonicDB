using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Columns.ULongColumns;
using Reloaded.Memory.Extensions;
using Xunit.DependencyInjection;

namespace NexusMods.EventSourcing.Storage.Tests.ColumnTests;

public class ULongColumnTests
{
    [Fact]
    public void ConstantColumnsPackIntoSingleValue()
    {
        var column = (ICanBePacked<ulong>)Create(42, 42, 42);
        var packed = column.Pack();

        packed.GetType().Should().Be(typeof(Constant<ulong>));
        AssertEqual((IReadable<ulong>)column, packed);
    }

    [Fact]
    public void EmptyColumnsPackIntoConstant()
    {
        var column = (ICanBePacked<ulong>)Create();
        var packed = column.Pack();

        packed.GetType().Should().Be(typeof(Constant<ulong>));
        AssertEqual((IReadable<ulong>)column, packed);
    }

    [Fact]
    public void MultipleValuesColumnsPackIntoMinMax()
    {
        var column = (ICanBePacked<ulong>)Create(42, 43, 44);
        var packed = column.Pack();

    }

    [Theory]
    [MethodData(nameof(TestData))]
    public Task CanGetColumnStatistics(string comment, ulong[] values)
    {
        var column = (ICanBePacked<ulong>)Create(values);
        var stats = column.GetStatistics();
        return Verify(stats).UseTextForParameters(comment);
    }

    [Theory]
    [MethodData(nameof(TestData))]
    public void PackedDataShouldRoundTrip(string name, ulong[] values)
    {
        var column = (ICanBePacked<ulong>)Create(values);
        var packed = column.Pack();
        AssertEqual(packed, (IReadable<ulong>)column);


        var unpacked = packed.Unpack();
        AssertEqual(unpacked, (IReadable<ulong>)column);
    }

    [Theory]
    [MethodData(nameof(TestData))]
    public Task PackedDataShouldHaveCorrectHeaders(string name, ulong[] data)
    {
        var column = (ICanBePacked<ulong>)Create(data);
        var packed = column.Pack();
        if (packed is Constant<ulong>)
            return Task.CompletedTask;

        var casted = (OnHeapPacked<ulong>)packed;
        var header = casted.LowLevel;

        var values = new List<ulong>();
        switch (header.ValueBytes)
        {
            case 1:
                for (var i = 0; i < header.Length; i++)
                {
                    values.Add(casted.Data[i]);
                }
                break;
            case 2:
                for (var i = 0; i < header.Length; i++)
                {
                    values.Add(MemoryMarshal.Read<ushort>(casted.Data.SliceFast(i * 2)));
                }
                break;

            default:
                throw new NotSupportedException();

        }
        return Verify(new
            {
                Header = casted.LowLevel,
                Values = values.ToArray()
            })
            .UseTextForParameters(name);
    }


    private Appendable<ulong> Create(params ulong[] values)
    {
        var column = Appendable<ulong>.Create(values.Length);
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
            ("Two byte values", [1, 1024])

        };

        foreach (var (name, values) in data)
        {
            yield return [name, values];
        }
    }

    private void AssertEqual<T>(IReadable<T> a, IReadable<T> b)
        where T : struct
    {
        a.Length.Should().Be(b.Length, "the columns should have the same length.");
        for (var i = 0; i < a.Length; i++)
        {
            Ids.GetPartition(Unsafe.BitCast<T, ulong>(a[i])).Should().Be(Ids.GetPartition(Unsafe.BitCast<T, ulong>(b[i])), "the columns should have the same partition at index " + i);
            a[i].Should().Be(b[i], "the columns should have the same values at index " + i);
        }
    }
}
