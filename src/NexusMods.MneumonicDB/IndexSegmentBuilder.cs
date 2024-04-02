using System;
using System.Buffers;
using System.Collections.Generic;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Storage;

namespace NexusMods.MneumonicDB;

/// <summary>
/// A builder for constructing an index segment
/// </summary>
public struct IndexSegmentBuilder
{
    private List<int> _offsets;
    private PooledMemoryBufferWriter _data;

    /// <summary>
    /// Create a new index segment builder
    /// </summary>
    public IndexSegmentBuilder(int capacity = 1024)
    {
        _offsets = new List<int>();
        _data = new PooledMemoryBufferWriter(capacity);
    }

    /// <summary>
    /// Add a datom to the segment
    /// </summary>
    public void Add(ReadOnlySpan<byte> datom)
    {
        _offsets.Add(_data.Length);
        _data.Write(datom);
    }

    /// <summary>
    /// Construct the index segment
    /// </summary>
    public IndexSegment Build()
    {
        _offsets.Add(_data.Length);
        return new IndexSegment(_data.GetWrittenSpan(), _offsets.ToArray());
    }
}
