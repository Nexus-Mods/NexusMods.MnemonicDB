using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.Columns.PackedColumns;

public class ConstantPackedColumn<TElement, TPack> : IPackedColumn<TElement>
    where TElement : unmanaged
    where TPack : unmanaged
{
    private readonly int _length;
    private readonly TPack _value;

    public ConstantPackedColumn(int length, TElement value)
    {
        _length = length;
        _value = Unsafe.BitCast<TElement, TPack>(value);
    }

    public TElement this[int index] => Unsafe.BitCast<TPack, TElement>(_value);

    public int Length => _length;
    public IColumn<TElement> Pack()
    {
        return this;
    }

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        if (typeof(TPack) == typeof(byte))
        {
            writer.WriteFourCC(FourCC.ConstByte);
            writer.Write(Unsafe.BitCast<TPack, byte>(_value));
            return;
        }

        if (typeof(TPack) == typeof(ushort))
        {
            writer.WriteFourCC(FourCC.ConstUShort);
            writer.Write(Unsafe.BitCast<TPack, ushort>(_value));
            return;
        }

        if (typeof(TPack) == typeof(uint))
        {
            writer.WriteFourCC(FourCC.ConstUInt);
            writer.Write(Unsafe.BitCast<TPack, uint>(_value));
            return;
        }

        if (typeof(TPack) == typeof(ulong))
        {
            writer.WriteFourCC(FourCC.ConstULong);
            writer.Write(Unsafe.BitCast<TPack, ulong>(_value));
            return;
        }

        throw new NotSupportedException($"Unsupported constant packed column type: {typeof(TPack)}");
    }

    public void CopyTo(Span<TElement> destination)
    {
        for (var i = 0; i < _length; i++)
        {
            destination[i] = this[i];
        }
    }

    public static IColumn<TElement> ReadFrom(ref BufferReader data, int length)
    {
        var value = data.Read<TPack>();
        return new ConstantPackedColumn<TElement, TPack>(length, Unsafe.BitCast<TPack, TElement>(value));
    }
}
