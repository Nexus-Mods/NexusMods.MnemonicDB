using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore.BuiltInSerializers;

/// <inheritdoc />
public class BoolSerializer : IValueSerializer<byte>
{
    /// <inheritdoc />
    public Type NativeType => typeof(byte);

    public static readonly UInt128 Id = "50BECA70-43D9-497D-B47C-8AD8B85B7801".ToUInt128Guild();

    /// <inheritdoc />
    public UInt128 UniqueId => Id;

    /// <inheritdoc />
    public bool TryGetFixedSize(out int size)
    {
        size = 1;
        return true;
    }

    /// <inheritdoc />
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return a[0].CompareTo(b[0]);
    }

    /// <inheritdoc />
    public void Write<TWriter>(byte value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(1);
        span[0] = value;
        buffer.Advance(1);
    }

    /// <inheritdoc />
    public int Read(ReadOnlySpan<byte> buffer, out byte val)
    {
        val = buffer[0];
        return 1;
    }
}
