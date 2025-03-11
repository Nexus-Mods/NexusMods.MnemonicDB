using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.Paths.Utilities;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A subview of an IndexSegment that returns a specific value type
/// </summary>
public readonly struct Values<TValueType>(EntitySegment segment, Range range, IReadableAttribute<TValueType> attribute) :
    IEnumerable<TValueType>, IIndexSegment<TValueType> 
{
    /// <summary>
    /// Gets the value at the given location
    /// </summary>
    public TValueType this[int idx]
    {
        get
        {
            if (idx < 0 || idx >= Count)
            {
                throw new IndexOutOfRangeException();
            }
            
            if (!segment.TryGetValue<IReadableAttribute<TValueType>, TValueType>(attribute, range.Start.Value + idx, out var value))
            {
                throw new IndexOutOfRangeException();
            }
            return value;
        }
    }

    /// <summary>
    /// Returns the number of items in the collection
    /// </summary>
    public int Count => range.End.Value - range.Start.Value;

    /// <summary>
    /// Converts the view to an array
    /// </summary>
    public TValueType[] ToArray()
    {
        var arr = GC.AllocateUninitializedArray<TValueType>(Count);
        for (var i = 0; i < Count; i++)
        {
            arr[i] = this[i];
        }
        return arr;
    }

    /// <inheritdoc />
    public IEnumerator<TValueType> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
