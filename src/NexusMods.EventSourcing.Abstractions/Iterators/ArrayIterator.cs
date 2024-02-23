using System.Collections.Generic;

namespace NexusMods.EventSourcing.Abstractions.Iterators;

/// <summary>
/// Iterator over an array of values.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ArrayIterator<T> : IIterator<T>
{
    private readonly IReadOnlyList<T> _array;
    private int _idx;

    /// <summary>
    /// Iterator over an array of values.
    /// </summary>
    /// <param name="array"></param>
    /// <param name="idx"></param>
    /// <typeparam name="T"></typeparam>
    public ArrayIterator(IReadOnlyList<T> array, int idx)
    {
        _array = array;
        _idx = idx;
    }

    /// <inheritdoc />
    public bool Next()
    {
        if (_idx == _array.Count) return false;
        var newIdx = _idx + 1;
        if (newIdx == _array.Count)
        {
            _idx = _array.Count;
            return false;
        }
        _idx = newIdx;
        return true;
    }

    /// <inheritdoc />
    public bool AtEnd => _idx >= _array.Count;

    /// <inheritdoc />
    public bool Value(out T value)
    {
        if (_idx >= _array.Count)
        {
            value = default!;
            return false;
        }
        value = _array[_idx];
        return true;
    }
}
