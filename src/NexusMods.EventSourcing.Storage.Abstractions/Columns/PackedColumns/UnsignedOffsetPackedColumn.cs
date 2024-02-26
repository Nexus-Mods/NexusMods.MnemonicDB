using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.EventSourcing.Storage.Abstractions.Columns.PackedColumns;

public class UnsignedOffsetPackedColumn<TElement, TPack> : IPackedColumn<TElement>
    where TPack : unmanaged, IBinaryInteger<TPack>
    where TElement : unmanaged, IBinaryInteger<TElement>
{
    private TElement _offset;
    private int _length;
    private TPack[] _data;

    public UnsignedOffsetPackedColumn(IUnpackedColumn<TElement> elements, TElement offset)
    {
        _offset = offset;
        _length = elements.Length;
        _data = GC.AllocateUninitializedArray<TPack>(_length);

        for (var i = 0; i < _length; i++)
        {
            _data[i] = TPack.CreateTruncating(elements[i] - offset);
        }
    }


    public TElement this[int index] => TElement.CreateTruncating(_data[index]) + _offset;

    public int Length => _length;

    public void CopyTo(Span<TElement> destination)
    {
        throw new NotImplementedException();

    }
}
