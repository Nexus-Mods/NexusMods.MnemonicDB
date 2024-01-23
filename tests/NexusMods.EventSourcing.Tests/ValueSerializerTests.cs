using System.Buffers;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Tests;

public class ValueSerializerTests
{
    private readonly IEnumerable<ISerializer> _serializers;

    public ValueSerializerTests(IEnumerable<ISerializer> serializers)
    {
        _serializers = serializers;
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void SerializeValue(string typeName, object value)
    {
        var serializer = _serializers.FirstOrDefault(s => s.CanSerialize(value.GetType()));
        serializer.Should().NotBeNull();
        if (serializer == default) return;

        serializer.CanSerialize(value.GetType()).Should().BeTrue();

        var methodInfo = typeof(ValueSerializerTests).GetMethod(nameof(TestSerializer), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        methodInfo = methodInfo!.MakeGenericMethod(value.GetType());
        methodInfo.Invoke(this, [serializer, value]);
    }

    private void TestSerializer<T>(ISerializer serializer, object t)
    {
        if(serializer is IFixedSizeSerializer<T> fixedSizeSerializer)
        {
            fixedSizeSerializer.TryGetFixedSize(typeof(T), out var size).Should().BeTrue();
            var span = new byte[size];
            fixedSizeSerializer.Serialize((T)t, span);
            var deserialized = fixedSizeSerializer.Deserialize(span);
            deserialized.Should().Be(t);
        }
        else if (serializer is IVariableSizeSerializer<T> variableSizeSerializer)
        {
            var writer = new ArrayBufferWriter<byte>();
            variableSizeSerializer.Serialize((T)t, writer);
            var deserializedSize = variableSizeSerializer.Deserialize(writer.WrittenSpan, out var read);
            deserializedSize.Should().Be(writer.WrittenCount);
            read.Should().Be(t);
        }
        else
        {
            throw new Exception("Unknown serializer type");
        }
    }


    public static IEnumerable<object[]> Data = new object[]
    {
        "Test",
        (byte)1,
        (short)1,
        (int)1,
        (long)1,
        (float)1,
        (double)1,
        (ushort)1,
        (uint)1,
        (ulong)1,
        Guid.Parse("154C9597-9E14-41A8-BFB9-2AEA27CA534B"),
    }.Select(v => new[] {v.GetType().Name, v }).ToList();

}
