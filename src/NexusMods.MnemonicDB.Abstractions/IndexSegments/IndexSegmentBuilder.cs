﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
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

    /// <summary>
    /// Not supported, use the other constructor
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
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
    /// Adds an assert datom to the segment for the tmp transaction
    /// </summary>
    public void Add<TValue, TLowLevel, TSerializer>(EntityId entityId,
        Attribute<TValue, TLowLevel, TSerializer> attribute, TValue value, bool isRetract = false)
        where TSerializer : IValueSerializer<TLowLevel>
    {
        _offsets.Add(_data.Length);
        attribute.Write(entityId, _attributeCache, value, TxId.Tmp, isRetract, _data);
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
    /// Adds the current item pointed to by the enumerator
    /// </summary>
    public void AddCurrent<T>(in T enumerator) where T : IRefDatomEnumerator, allows ref struct
    {
        _offsets.Add(_data.Length);
        var prefix = enumerator.KeyPrefix;
        var prefixSpan = _data.GetSpan(KeyPrefix.Size);
        MemoryMarshal.Write(prefixSpan, prefix);
        _data.Advance(KeyPrefix.Size);
        _data.Write(enumerator.ValueSpan);
        
        // Write the hashed blob if it exists
        if (prefix.ValueTag == ValueTag.HashedBlob) 
            _data.Write(enumerator.ExtraValueSpan);
    }

    /// <summary>
    /// Adds all the items from the enumerator to the segment
    /// </summary>
    public void AddRange<TOuter, TInner>(in TOuter enumerator) 
        where TOuter : IRefDatomEnumerable<TInner>, allows ref struct
        where TInner : IRefDatomEnumerator, allows ref struct
    {
        using var inner = enumerator.GetEnumerator();
        while (inner.MoveNext())
        {
            AddCurrent(in inner);
        }
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
