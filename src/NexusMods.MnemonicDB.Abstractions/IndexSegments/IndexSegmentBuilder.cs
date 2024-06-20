﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A builder for constructing an index segment
/// </summary>
public struct IndexSegmentBuilder : IDisposable
{
    private List<int> _offsets;
    private PooledMemoryBufferWriter _data;
    private readonly RegistryId _registryId;
    private readonly IAttributeRegistry _registry;

    /// <summary>
    /// Create a new index segment builder
    /// </summary>
    public IndexSegmentBuilder(IAttributeRegistry registry, int capacity = 1024)
    {
        _registry = registry;
        _registryId = registry.Id;
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
    /// Add a datom to the segment
    /// </summary>
    public readonly void Add<TValue, TLowLevelType>(EntityId entityId, Attribute<TValue, TLowLevelType> attribute, TValue value, TxId txId, bool isRetract)
    {
        _offsets.Add(_data.Length);
        attribute.Write(entityId, _registryId, value, txId, isRetract, _data);
    }

    /// <summary>
    /// Adds an assert datom to the segment for the tmp transaction
    /// </summary>
    public readonly void Add<TValue, TLowLevelType>(EntityId entityId, Attribute<TValue, TLowLevelType> attribute, TValue value)
    {
        _offsets.Add(_data.Length);
        attribute.Write(entityId, _registryId, value, TxId.Tmp, false, _data);
    }

    /// <summary>
    /// Adds a datom to the segment for the tmp transaction, with the given assert flag
    /// </summary>
    public readonly void Add<TValue, TLowLevelType>(EntityId entityId, Attribute<TValue, TLowLevelType> attribute, TValue value, bool isRetract)
    {
        _offsets.Add(_data.Length);
        attribute.Write(entityId, _registryId, value, TxId.Tmp, isRetract, _data);
    }

    /// <summary>
    /// Append
    /// </summary>
    public readonly void Add(ReadOnlySpan<byte> rawData)
    {
        Debug.Assert(rawData.Length >= KeyPrefix.Size, "Raw data must be at least the size of a KeyPrefix");
        _offsets.Add(_data.Length);
        _data.Write(rawData);
    }

    /// <summary>
    /// Construct the index segment
    /// </summary>
    public readonly IndexSegment Build()
    {
        _offsets.Add(_data.Length);
        return new IndexSegment(_data.GetWrittenSpan(), _offsets.ToArray(), _registry);
    }

    /// <inheritdoc />
    public readonly void Dispose()
    {
        _data.Dispose();
    }
}
