using System;
using System.Buffers;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB;

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
    public void Add(IEnumerable<Datom> datoms)
    {
        foreach (var datom in datoms)
        {
            _offsets.Add(_data.Length);
            _data.Write(datom.RawSpan);
        }
    }

    /// <summary>
    /// Construct the index segment
    /// </summary>
    public IndexSegment Build(IAttributeRegistry registry)
    {
        _offsets.Add(_data.Length);
        return new IndexSegment(_data.GetWrittenSpan(), _offsets.ToArray(), registry);
    }
}
