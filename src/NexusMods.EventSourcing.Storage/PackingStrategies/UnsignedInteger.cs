using System;
using System.Numerics;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Abstractions.Columns.PackedColumns;
using NexusMods.EventSourcing.Storage.Columns;
using NexusMods.EventSourcing.Storage.Columns.PackedColumns;

namespace NexusMods.EventSourcing.Storage.Abstractions.PackingStrategies;

public static class UnsignedInteger
{


    public static IPackedColumn<TElement> Pack<TElement>(UnsignedIntegerColumn<TElement> elements)
        where TElement : unmanaged
    {
        var columnSize = 0;

        unsafe
        {
            columnSize = sizeof(TElement);
        }

        return columnSize switch
        {
            1 => PackUInt<TElement, byte>(elements),
            2 => PackUInt<TElement, ushort>(elements),
            4 => PackUInt<TElement, uint>(elements),
            8 => PackUInt<TElement, ulong>(elements),
            _ => throw new NotImplementedException()
        };
    }

    private static IPackedColumn<TElement> PackUInt<TElement, TInternal>(UnsignedIntegerColumn<TElement> elements)
        where TElement : unmanaged
        where TInternal : unmanaged, IBinaryInteger<TInternal>, IMinMaxValue<TInternal>
    {
        var span = MemoryMarshal.Cast<TElement, TInternal>(elements.Data);

        var max = TInternal.MinValue;
        var min = TInternal.MaxValue;
        var mask = TInternal.MinValue;

        for (var i = 0; i < span.Length; i++)
        {
            var element = span[i];
            max = element > max ? element : max;
            min = element < min ? element : min;
            mask |= element;
        }

        var range = max - min;

        if (range == TInternal.Zero)
        {
            return new ConstantPackedColumn<TElement, TInternal>(span.Length, elements.Data[0]);
        }
        else if (range <= TInternal.CreateTruncating(byte.MaxValue))
        {
            return new UnsignedOffsetPackedColumn<TElement, TInternal, byte>(elements, min);
        }
        else if (range <= TInternal.CreateTruncating(ushort.MaxValue))
        {
            return new UnsignedOffsetPackedColumn<TElement, TInternal, ushort>(elements, min);
        }
        else if (range <= TInternal.CreateTruncating(uint.MaxValue))
        {
            return new UnsignedOffsetPackedColumn<TElement, TInternal, uint>(elements, min);
        }
        else
        {
            return new UnsignedOffsetPackedColumn<TElement, TInternal, ulong>(elements, min);
        }

    }
}
