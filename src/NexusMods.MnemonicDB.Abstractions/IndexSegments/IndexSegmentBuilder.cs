using System;
using System.Collections.Generic;
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
    public readonly void Add<TValue>(EntityId entityId, Attribute<TValue> attribute, TValue value, TxId txId, bool isRetract)
    {
        _offsets.Add(_data.Length);
        attribute.Write(entityId, _registryId, value, txId, isRetract, _data);
    }

    /// <summary>
    /// Adds an assert datom to the segment for the tmp transaction
    /// </summary>
    public readonly void Add<TValue>(EntityId entityId, Attribute<TValue> attribute, TValue value)
    {
        _offsets.Add(_data.Length);
        attribute.Write(entityId, _registryId, value, TxId.Tmp, false, _data);
    }

    /// <summary>
    /// Adds a datom to the segment for the tmp transaction, with the given assert flag
    /// </summary>
    public readonly void Add<TValue>(EntityId entityId, Attribute<TValue> attribute, TValue value, bool isRetract)
    {
        _offsets.Add(_data.Length);
        attribute.Write(entityId, _registryId, value, TxId.Tmp, isRetract, _data);
    }

    /// <summary>
    /// Construct the index segment
    /// </summary>
    public IndexSegment Build()
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
