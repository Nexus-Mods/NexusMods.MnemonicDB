using System;
using System.Buffers;
using System.Buffers.Binary;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.BuiltInSerializers;

/// <summary>
/// A <see cref="UInt128"/> serializer
/// </summary>
public class UInt128Serializer : IValueSerializer<UInt128>
{
    /// <inheritdoc />
    public Type NativeType => typeof(UInt128);

    public static readonly UInt128 Id = "50BECA70-43D9-497D-B47C-8AD8B85B7803".ToUInt128Guild();

    /// <inheritdoc />
    public UInt128 UniqueId => Id;

    /// <inheritdoc />
    public bool TryGetFixedSize(out int size)
    {
        size = 16;
        return true;
    }

    /// <inheritdoc />
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var aVal = BinaryPrimitives.ReadUInt128LittleEndian(a);
        var bVal = BinaryPrimitives.ReadUInt128LittleEndian(b);
        return aVal.CompareTo(bVal);
    }

    /// <inheritdoc />
    public void Write<TWriter>(UInt128 value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        var span = buffer.GetSpan(16);
        BinaryPrimitives.WriteUInt128LittleEndian(span, value);
        buffer.Advance(16);
    }

    /// <inheritdoc />
    public int Read(ReadOnlySpan<byte> buffer, out UInt128 val)
    {
        val = BinaryPrimitives.ReadUInt128LittleEndian(buffer);
        return 16;
    }
}
