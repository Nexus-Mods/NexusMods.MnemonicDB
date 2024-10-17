﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A builder for constructing an index segment
/// </summary>
public readonly struct IndexSegmentBuilder : IDisposable
{
    private readonly List<int> _offsets;
    private readonly PooledMemoryBufferWriter _data;
    private readonly AttributeCache _attributeCache;

    public IndexSegmentBuilder()
    {
        throw new NotSupportedException();
    }
    
    /// <summary>
    /// Create a new index segment builder
    /// </summary>
    public IndexSegmentBuilder(AttributeCache attributeCache, int capacity = 1024)
    {
        _attributeCache = attributeCache;
        _offsets = new List<int>();
        _data = new PooledMemoryBufferWriter(capacity);
    }

    /// <summary>
    /// The number of datoms in the segment
    /// </summary>
    public int Count => _offsets.Count;

    /// <summary>
    /// Resets the builder so it can be reused
    /// </summary>
    public void Reset()
    {
        _offsets.Clear();
        _data.Reset();
    }

    /// <summary>
    /// Add the datoms to the segment
    /// </summary>
    public void Add(IEnumerable<Datom> datoms)
    {
        foreach (var datom in datoms)
        {
            Add(datom);
        }
    }

    /// <summary>
    /// Add a datom to the segment
    /// </summary>
    /// <param name="datom"></param>
    public void Add(in Datom datom)
    {
        _offsets.Add(_data.Length);
        var span = _data.GetSpan(KeyPrefix.Size);
        MemoryMarshal.Write(span, datom.Prefix);
        _data.Advance(KeyPrefix.Size);
        _data.Write(datom.ValueSpan);
    }

    /// <summary>
    /// Add a datom to the segment
    /// </summary>
    public void Add<TValue, TAttribute>(EntityId entityId, TAttribute attribute, TValue value, TxId txId, bool isRetract)
    where TAttribute : IWritableAttribute<TValue>
    {
        _offsets.Add(_data.Length);
        attribute.Write(entityId, _attributeCache, value, txId, isRetract, _data);
    }

    /// <summary>
    /// Adds an assert datom to the segment for the tmp transaction
    /// </summary>
    public void Add<TValue, TAttribute>(EntityId entityId, TAttribute attribute, TValue value)
        where TAttribute : IWritableAttribute<TValue>
    {
        _offsets.Add(_data.Length);
        attribute.Write(entityId, _attributeCache, value, TxId.Tmp, false, _data);
    }

    /// <summary>
    /// Adds a datom to the segment for the tmp transaction, with the given assert flag
    /// </summary>
    public void Add<TValue, TAttribute>(EntityId entityId, TAttribute attribute, TValue value, bool isRetract)
    where TAttribute : IWritableAttribute<TValue>
    {
        _offsets.Add(_data.Length);
        attribute.Write(entityId, _attributeCache, value, TxId.Tmp, isRetract, _data);
    }

    /// <summary>
    /// Append
    /// </summary>
    public void Add(ReadOnlySpan<byte> rawData)
    {
        Debug.Assert(rawData.Length >= KeyPrefix.Size, "Raw data must be at least the size of a KeyPrefix");
        _offsets.Add(_data.Length);
        _data.Write(rawData);
    }

    /// <summary>
    /// Construct the index segment
    /// </summary>
    public IndexSegment Build()
    {
        _offsets.Add(_data.Length);
        return new IndexSegment(_data.GetWrittenSpan(), _offsets.ToArray(), _attributeCache);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _data.Dispose();
    }
}
