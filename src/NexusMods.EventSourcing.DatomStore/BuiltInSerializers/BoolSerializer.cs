using System;
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore.BuiltInSerializers;

/// <inheritdoc />
public class BoolSerializer : IValueSerializer<bool>
{
    /// <inheritdoc />
    public Type NativeType => typeof(bool);

    public static readonly UInt128 Id = "50BECA70-43D9-497D-B47C-8AD8B85B7801".ToUInt128Guid();

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
    public void Write<TWriter>(bool value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(1);
        span[0] = value ? (byte) 1 : (byte) 0;
        buffer.Advance(1);
    }

    /// <inheritdoc />
    public int Read(ReadOnlySpan<byte> buffer, out bool val)
    {
        val = buffer[0] != 0;
        return 1;
    }
}
