using System;
using System.Collections;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A subview of an IndexSegment that returns a specific value type
/// </summary>
public struct Values<TValueType, TLowLevelType>(IndexSegment segment, int start, int end, Attribute<TValueType, TLowLevelType> attribute) :
    IEnumerable<TValueType>, IIndexSegment<TValueType>
{
    /// <summary>
    /// Gets the value at the given location
    /// </summary>
    public TValueType this[int idx]
    {
        get
        {
            var datom = segment[idx + start];
            return attribute.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, segment.RegistryId);
        }
    }

    /// <summary>
    /// Returns the number of items in the collection
    /// </summary>
    public int Count => end - start;

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
