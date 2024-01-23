using System;
using System.Buffers;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions.Serialization;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Serialization;

/// <summary>
/// Serializer for writing strings.
/// </summary>
public sealed class StringSerializer : AVariableSizeSerializer<string>
{
    /// <inheritdoc />
    public override void Serialize<TWriter>(string value, TWriter output)
    {
        var size = System.Text.Encoding.UTF8.GetByteCount(value);
        WriteLength(output, size);
        var span = output.GetSpan(size);
        System.Text.Encoding.UTF8.GetBytes(value, span);
        output.Advance(size);
    }

    /// <inheritdoc />
    public override int Deserialize(ReadOnlySpan<byte> from, out string value)
    {
        var read = ReadLength(from, out var size);
        value = System.Text.Encoding.UTF8.GetString(from.SliceFast(read, size));
        return size + read;
    }
}
