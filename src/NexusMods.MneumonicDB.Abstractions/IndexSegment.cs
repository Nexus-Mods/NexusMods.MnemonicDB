using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Win32;
using NexusMods.MneumonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB.Abstractions.DatomIterators;

/// <summary>
///  A segment of an index, used most often as a cache. For example when an entity is read from the database,
/// the whole entity may be cached in one of these segments for fast access.
/// </summary>
public readonly struct IndexSegment : IEnumerable<Datom>
{
    private readonly Memory<byte> _data;
    private readonly Memory<int> _offsets;
    private readonly IAttributeRegistry _registry;

    /// <summary>
    /// Construct a new index segment from the given data and offsets
    /// </summary>
    public IndexSegment(ReadOnlySpan<byte> data, ReadOnlySpan<int> offsets, IAttributeRegistry registry)
    {
        _registry = registry;
        _data = data.ToArray();
        _offsets = offsets.ToArray();
    }

    /// <summary>
    /// The number of datoms in this segment
    /// </summary>
    public int Count => _offsets.Length - 1;

    /// <summary>
    /// Get the datom of the given index
    /// </summary>
    public Datom this[int idx]
    {
        get
        {
            var fromOffset = _offsets.Span[idx];
            return new Datom(_data.Slice(fromOffset, _offsets.Span[idx + 1] - fromOffset), _registry);
        }
    }

    /// <inheritdoc />
    public IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < _offsets.Length - 1; i++)
        {
            var fromOffset = _offsets.Span[i];
            yield return new Datom(_data.Slice(fromOffset, _offsets.Span[i + 1] - fromOffset), _registry);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
