using System;
using System.Buffers;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;

namespace NexusMods.EventSourcing.Serialization;

public class StringSerializer : IVariableSizeSerializer<string>
{
    public bool CanSerialize(Type valueType)
    {
        return valueType == typeof(string);
    }

    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 0;
        return false;
    }

    public void Serialize<TWriter>(string value, TWriter output) where TWriter : IBufferWriter<byte>
    {
        var size = System.Text.Encoding.UTF8.GetByteCount(value);
        var span = output.GetSpan(size + 2);
        BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)size);
        System.Text.Encoding.UTF8.GetBytes(value, span[2..]);
        output.Advance(size + 2);
    }

    public int Deserialize(ReadOnlySpan<byte> from, out string value)
    {
        var size = BinaryPrimitives.ReadUInt16LittleEndian(from);
        value = System.Text.Encoding.UTF8.GetString(from[2..(2 + size)]);
        return size + 2;
    }
}
