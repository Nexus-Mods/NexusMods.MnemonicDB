using System;
using System.Numerics;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Abstractions.Columns.PackedColumns;

namespace NexusMods.EventSourcing.Storage.Abstractions.PackingStrategies;

public static class UnsignedInteger
{
    public static IPackedColumn<TElement> Pack<TElement>(IUnpackedColumn<TElement> elements)
        where TElement : unmanaged, IBinaryInteger<TElement>, IMinMaxValue<TElement>
    {
        var span = elements.Data;

        var max = TElement.MinValue;
        var min = TElement.MaxValue;

        // TODO: Vectorize this
        for(var i = 0; i < span.Length; i++)
        {
            var element = span[i];
            max = TElement.Max(max, element);
            min = TElement.Min(min, element);
        }

        var range = ulong.CreateTruncating(max - min);

        if (range == 0)
        {
            throw new NotImplementedException();
        }
        else if (range <= byte.MaxValue)
        {
            return new UnsignedOffsetPackedColumn<TElement, byte>(elements, min);
        }
        else if (range <= ushort.MaxValue)
        {
            return new UnsignedOffsetPackedColumn<TElement, ushort>(elements, min);
        }
        else if (range <= uint.MaxValue)
        {
            return new UnsignedOffsetPackedColumn<TElement, uint>(elements, min);
        }
        else
        {
            return new UnsignedOffsetPackedColumn<TElement, ulong>(elements, min);
        }

        throw new NotImplementedException();

    }

}
