﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A subview of an IndexSegment that returns the entity ids of the segment
/// </summary>
public struct EntityIds(IndexSegment segment, int start, int end) :
    IEnumerable<EntityId>, IIndexSegment<EntityId>
{
    /// <summary>
    /// Gets the value at the given location
    /// </summary>
    public EntityId this[int idx] => segment[idx + start].E;

    /// <summary>
    /// Returns the number of items in the collection
    /// </summary>
    public int Count => end - start;

    /// <summary>
    /// Converts the view to an array
    /// </summary>
    public EntityId[] ToArray()
    {
        var arr = GC.AllocateUninitializedArray<EntityId>(Count);
        for (var i = 0; i < Count; i++)
        {
            arr[i] = this[i];
        }
        return arr;
    }

    /// <inheritdoc />
    public IEnumerator<EntityId> GetEnumerator()
    {
        for (var i = start; i < end; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
