using System.Buffers;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Tests;

public class ValueSerializerTests
{
    private readonly ISerializationRegistry _registry;


    public ValueSerializerTests(ISerializationRegistry registry)
    {
        _registry = registry;
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void SerializeValue(string typeName, object value)
    {
        var method = typeof(ValueSerializerTests).GetMethod(nameof(TestSerializer), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var generic = method!.MakeGenericMethod(value.GetType());
        generic.Invoke(this, [value]);
    }

    private void TestSerializer<T>(T value)
    {
        var writer = new ArrayBufferWriter<byte>();
        _registry.Serialize(writer, value);
        var read = _registry.Deserialize<T>(writer.WrittenSpan, out var deserialized);
        read.Should().Be(writer.WrittenCount);
        deserialized.Should().BeEquivalentTo(value, opts => opts.RespectingRuntimeTypes());

    }



    public static IEnumerable<object[]> Data = new object[]
    {
        "Test",
        Enumerable.Range(0, 500).ToArray(),
        string.Join("", Enumerable.Range(0, 500).Select(f => f.ToString())),
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
