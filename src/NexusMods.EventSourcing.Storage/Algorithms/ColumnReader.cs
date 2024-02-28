using System;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Abstractions.Columns.PackedColumns;
using NexusMods.EventSourcing.Storage.Columns.PackedColumns;

namespace NexusMods.EventSourcing.Storage.Algorithms;

public static class ColumnReader
{
    public static IColumn<T> ReadColumn<T>(ref BufferReader src, int length) where T : unmanaged
    {
        var header = src.ReadFourCC();

        if (header == FourCC.OffsetULongAsUShort)
        {
            return UnsignedOffsetPackedColumn<T, ulong, ushort>.ReadFrom(ref src, length);
        }
        if (header == FourCC.OffsetULongAsByte)
        {
            return UnsignedOffsetPackedColumn<T, ulong, byte>.ReadFrom(ref src, length);
        }
        if (header == FourCC.ConstByte)
        {
            return ConstantPackedColumn<T, byte>.ReadFrom(ref src, length);
        }

        if (header == FourCC.ConstULong)
        {
            return ConstantPackedColumn<T, ulong>.ReadFrom(ref src, length);
        }

        if (header == FourCC.OffsetUIntAsUInt)
        {
            return UnsignedOffsetPackedColumn<T, uint, uint>.ReadFrom(ref src, length);
        }

        if (header == FourCC.OffsetULongAsUInt)
        {
            return UnsignedOffsetPackedColumn<T, ulong, uint>.ReadFrom(ref src, length);
        }

        if (header == FourCC.OffsetByteAsByte)
        {
            return UnsignedOffsetPackedColumn<T, byte, byte>.ReadFrom(ref src, length);
        }

        if (header == FourCC.ConstUInt)
        {
            return ConstantPackedColumn<T, uint>.ReadFrom(ref src, length);
        }

        if (header == FourCC.OffsetUIntAsUShort)
        {
            return UnsignedOffsetPackedColumn<T, uint, ushort>.ReadFrom(ref src, length);
        }

        if (header == FourCC.OffsetUIntAsByte)
        {
            return UnsignedOffsetPackedColumn<T, uint, byte>.ReadFrom(ref src, length);
        }


        throw new InvalidOperationException($"Unknown column type: {header}");
    }

    public static IBlobColumn ReadBlobColumn(ref BufferReader src, int length)
    {
        var header = src.ReadFourCC();
        if (header == FourCC.PackedBlob)
        {
            return PackedBlobColumn.ReadFrom(ref src, length);
        }
        else
        {
            throw new InvalidOperationException($"Unknown column type: {header}");
        }
    }

    public static IBlobColumn ReadBlobColumn(ref BufferReader src)
    {
        throw new NotImplementedException();
    }
}
