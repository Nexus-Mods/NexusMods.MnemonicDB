using System;
using System.Buffers;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for writing strings.
/// </summary>
public class StringSerializer : IVariableSizeSerializer<string>
{
    /// <inheritdoc />
    public bool CanSerialize(Type valueType)
    {
        return valueType == typeof(string);
    }

    /// <inheritdoc />
    public bool TryGetFixedSize(Type valueType, out int size)
    {
        size = 0;
        return false;
    }

    /// <inheritdoc />
    public void Serialize<TWriter>(string value, TWriter output) where TWriter : IBufferWriter<byte>
    {
        var size = System.Text.Encoding.UTF8.GetByteCount(value);
        var span = output.GetSpan(size + 2);
        BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)size);
        System.Text.Encoding.UTF8.GetBytes(value, span.SliceFast(2));
        output.Advance(size + 2);
    }

    /// <inheritdoc />
    public int Deserialize(ReadOnlySpan<byte> from, out string value)
    {
        var size = BinaryPrimitives.ReadUInt16LittleEndian(from);
        value = System.Text.Encoding.UTF8.GetString(from.SliceFast(2, size));
        return size + 2;
    }
}
