using System.Buffers.Binary;
using System.Linq.Expressions;
using System.Reflection;
using NexusMods.EventSourcing.Serializers;

namespace NexusMods.EventSourcing.Tests.SerializerTests;

public class IntSerializerTests
{

    [Theory]
    [InlineData(byte.MaxValue)]
    [InlineData(42)]
    [InlineData(byte.MinValue)]
    public void CanHandleInt8(byte val)
    {
        var serializer = new Int8Serializer();
        var span = new Span<byte>(new byte[1]);
        serializer.Serialize(ref span, ref val);
        span[0].Should().Be(val);

        var val2 = byte.MinValue;
        serializer.Deserialize(ref span, ref val2);
        val2.Should().Be(val);

    }

    [Theory]
    [InlineData(uint.MaxValue)]
    [InlineData(int.MaxValue)]
    [InlineData(short.MaxValue)]
    [InlineData(byte.MaxValue)]
    [InlineData(0)]
    public void CanHandleUInt32(uint val)
    {
        var serializer = new UInt32Serializer();
        var span = new Span<byte>(new byte[4]);
        serializer.Serialize(ref span, ref val);
        BinaryPrimitives.ReadUInt32BigEndian(span).Should().Be(val);

        var val2 = uint.MinValue;
        serializer.Deserialize(ref span, ref val2);
        val2.Should().Be(val);
    }

}
