using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions.Columns.PackedColumns;

public class UnsignedOffsetPackedColumn<TElement, TInternal, TPack> : IPackedColumn<TElement>
    where TPack : unmanaged, IBinaryInteger<TPack>
    where TInternal : unmanaged, IBinaryInteger<TInternal>
    where TElement : unmanaged
{
    private TInternal _offset;
    private int _length;
    private ReadOnlyMemory<byte> _data;

    public UnsignedOffsetPackedColumn(IUnpackedColumn<TElement> elements, TInternal offset)
    {
        _offset = offset;
        _length = elements.Length;
        unsafe
        {
            var data = GC.AllocateUninitializedArray<byte>(_length * sizeof(TPack));

            var casted = MemoryMarshal.Cast<TElement, TInternal>(elements.Data);
            var span = MemoryMarshal.Cast<byte, TPack>(data);

            for (var i = 0; i < _length; i++)
            {
                span[i] = TPack.CreateTruncating(casted[i] - offset);
            }

            _data = data;
        }
    }

    public UnsignedOffsetPackedColumn(int length, TInternal offset, ReadOnlyMemory<byte> data)
    {
        _offset = offset;
        _length = length;
        _data = data;
    }


    public TElement this[int index]
    {
        get
        {
            var dataSpan = MemoryMarshal.Cast<byte, TPack>(_data.Span);

            return Unsafe.BitCast<TInternal, TElement>(TInternal.CreateTruncating(dataSpan[index]) + _offset);
        }
    }

    public int Length => _length;
    public IColumn<TElement> Pack()
    {
        return this;
    }

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        switch ((typeof(TInternal), typeof(TPack)))
        {
            case ({ } t1, { } t2) when t1 == typeof(ulong) && t2 == typeof(byte):
            {
                writer.WriteFourCC(FourCC.OffsetULongAsByte);
                writer.Write(_offset);
                writer.Write(_data.Span);
                break;
            }

            case ({ } t1, { } t2) when t1 == typeof(ulong) && t2 == typeof(ushort):
            {
                writer.WriteFourCC(FourCC.OffsetULongAsUShort);
                writer.Write(_offset);
                writer.Write(_data.Span);
                break;
            }

            case ({ } t1, { } t2) when t1 == typeof(ulong) && t2 == typeof(uint):
            {
                writer.WriteFourCC(FourCC.OffsetULongAsUInt);
                writer.Write(_offset);
                writer.Write(_data.Span);
                break;
            }

            case ({ } t1, { } t2) when t1 == typeof(ulong) && t2 == typeof(ulong):
            {
                writer.WriteFourCC(FourCC.OffsetULongAsULong);
                writer.Write(_offset);
                writer.Write(_data.Span);
                break;
            }

            case ({ } t1, { } t2) when t1 == typeof(uint) && t2 == typeof(uint):
            {
                writer.WriteFourCC(FourCC.OffsetUIntAsUInt);
                writer.Write(_offset);
                writer.Write(_data.Span);
                break;
            }

            case ({ } t1, { } t2) when t1 == typeof(uint) && t2 == typeof(ushort):
            {
                writer.WriteFourCC(FourCC.OffsetUIntAsUShort);
                writer.Write(_offset);
                writer.Write(_data.Span);
                break;
            }

            case ({ } t1, { } t2) when t1 == typeof(uint) && t2 == typeof(byte):
            {
                writer.WriteFourCC(FourCC.OffsetUIntAsByte);
                writer.Write(_offset);
                writer.Write(_data.Span);
                break;
            }

            case ({ } t1, { } t2) when t1 == typeof(byte) && t2 == typeof(byte):
            {
                writer.WriteFourCC(FourCC.OffsetByteAsByte);
                writer.Write(_offset);
                writer.Write(_data.Span);
                break;
            }

            default:
                throw new NotSupportedException("Unsupported type combination: " + (typeof(TInternal), typeof(TPack)));
        }

    }

    public void CopyTo(Span<TElement> destination)
    {
        var dataSpan = MemoryMarshal.Cast<byte, TPack>(_data.Span);
        for (var i = 0; i < _length; i++)
        {
            destination[i] = Unsafe.BitCast<TInternal, TElement>(TInternal.CreateTruncating(dataSpan[i]) + _offset);
        }
    }

    public static IColumn<TElement> ReadFrom(ref BufferReader src, int length)
    {
        var offset = src.Read<TInternal>();
        unsafe
        {
            var data = src.ReadMemory(length * sizeof(TPack));
            return new UnsignedOffsetPackedColumn<TElement, TInternal, TPack>(length, offset, data);
        }

    }
}
