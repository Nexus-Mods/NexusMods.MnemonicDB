using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
///  A segment of an index, used most often as a cache. For example when an entity is read from the database,
/// the whole entity may be cached in one of these segments for fast access.
/// </summary>
[PublicAPI]
public readonly struct IndexSegment : IReadOnlyList<Datom>
{
    private readonly AttributeCache _attributeCache;
    private readonly int _rowCount;
    private readonly ReadOnlyMemory<byte> _data;

    /// <summary>
    /// Construct a new index segment from the given data and offsets
    /// </summary>
    public IndexSegment(ReadOnlySpan<byte> data, ReadOnlySpan<int> offsets, AttributeCache attributeCache)
    {
        _attributeCache = attributeCache;

        if (data.Length == 0)
        {
            _rowCount = 0;
            _data = ReadOnlyMemory<byte>.Empty;
            return;
        }

        _rowCount = offsets.Length - 1;

        var memory = new Memory<byte>(GC.AllocateUninitializedArray<byte>(data.Length + (_rowCount + 1) * sizeof(int)));
        _data = memory;

        ReprocessData(_rowCount, data, offsets, memory.Span);
    }
    
    /// <summary>
    /// Create an index segment from raw data
    /// </summary>
    public IndexSegment(int rowCount, ReadOnlyMemory<byte> data, AttributeCache attributeCache)
    {
        _attributeCache = attributeCache;
        _data = data;
        _rowCount = rowCount;
    }

    /// <summary>
    /// Gets read-only access to the data in this segment
    /// </summary>
    public ReadOnlyMemory<byte> Data => _data;
    
    /// <summary>
    /// All the upper values
    /// </summary>
    private ReadOnlySpan<ulong> Uppers => _data.Span.SliceFast(0, _rowCount * sizeof(ulong)).CastFast<byte, ulong>();

    /// <summary>
    /// All the lower values
    /// </summary>
    public ReadOnlySpan<ulong> Lowers => _data.Span.SliceFast(_rowCount * sizeof(ulong), _rowCount * sizeof(ulong)).CastFast<byte, ulong>();

    /// <summary>
    /// All the offsets
    /// </summary>
    private ReadOnlySpan<int> Offsets => _data.Span.SliceFast(_rowCount * sizeof(ulong) * 2, (_rowCount + 1) * sizeof(int)).CastFast<byte, int>();

    /// <summary>
    /// Pivots all the data into 4 columns:
    ///  - (ulong) upper part of the key prefix
    ///  - (ulong) lower part of the key prefix
    ///  - (int) offsets for each row's value into the value blob
    ///  - (byte[]) value blob
    /// </summary>
    private static void ReprocessData(int rowCount, ReadOnlySpan<byte> data, ReadOnlySpan<int> offsets, Span<byte> dataSpan)
    {
        var uppers = dataSpan.SliceFast(0, rowCount * sizeof(ulong)).CastFast<byte, ulong>();
        var lowers = dataSpan.SliceFast(rowCount * sizeof(ulong), rowCount * sizeof(ulong)).CastFast<byte, ulong>();

        // Extra space for one int in the offsets, so we can calculate the size of the last row
        var valueOffsets = dataSpan.SliceFast(rowCount * sizeof(ulong) * 2, (rowCount + 1) * sizeof(int)).CastFast<byte, int>();
        var values = dataSpan.SliceFast(rowCount * (sizeof(ulong) * 2 + sizeof(int)) + sizeof(int));

        var relativeValueOffset = 0;

        // The first row starts at the beginning of the value blob
        var absoluteValueOffset = rowCount * (sizeof(ulong) * 2 + sizeof(int)) + sizeof(int);

        for (var i = 0; i < rowCount; i++)
        {
            var rowSegment = data.Slice(offsets[i], offsets[i + 1] - offsets[i]);
            var prefix = MemoryMarshal.Read<KeyPrefix>(rowSegment);
            uppers[i] = prefix.Upper;
            lowers[i] = prefix.Lower;
            valueOffsets[i] = absoluteValueOffset;

            var valueSpan = rowSegment.SliceFast(KeyPrefix.Size);
            valueSpan.CopyTo(values.SliceFast(relativeValueOffset));

            relativeValueOffset += valueSpan.Length;
            absoluteValueOffset += valueSpan.Length;
        }

        // The last row's offset is the size of the value blob
        valueOffsets[rowCount] = absoluteValueOffset;
    }

    /// <summary>
    /// Returns true if this segment is valid (contains data)
    /// </summary>
    public bool Valid => !_data.IsEmpty;

    /// <summary>
    /// The size of the data in this segment, in bytes
    /// </summary>
    public Size DataSize => Size.FromLong(_data.Length);

    /// <summary>
    /// The number of datoms in this segment
    /// </summary>
    public int Count => _rowCount;
    
    /// <summary>
    /// Get the datom of the given index
    /// </summary>
    public Datom this[int idx]
    {
        get
        {
            var offsets = Offsets;
            var fromOffset = offsets[idx];
            var toOffset = offsets[idx + 1];

            var valueSlice = _data.Slice(fromOffset, toOffset - fromOffset);

            return new Datom(new KeyPrefix(Uppers[idx], Lowers[idx]), valueSlice);
        }
    }

    /// <summary>
    /// Returns true if the segment contains the given attribute
    /// </summary>
    public bool Contains(IAttribute attribute)
    {
        var id = _attributeCache.GetAttributeId(attribute.Id);
        foreach (var datom in this)
            if (datom.A == id)
                return true;
        return false;
    }

    /// <summary>
    /// Returns the enumerator.
    /// </summary>
    public Enumerator GetEnumerator() => new(this);

    /// <inheritdoc />
    IEnumerator<Datom> IEnumerable<Datom>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    /// <summary>
    /// Create a new index segment from the given datoms
    /// </summary>
    public static IndexSegment From(AttributeCache attributeCache, IReadOnlyCollection<Datom> datoms)
    {
        using var builder = new IndexSegmentBuilder(attributeCache, datoms.Count);
        builder.Add(datoms);
        return builder.Build();
    }

    /// <summary>
    /// Converts this index segment to am Entities segment, where each datom in this index becomes
    /// a loaded model. Assumes that datoms with duplicate entity ids should be loaded as separate models.
    /// </summary>
    public Entities<TModel> AsModels<TModel>(IDb fromDb) 
        where TModel : IReadOnlyModel<TModel>
    {
        return new Entities<TModel>(EntityIds(), fromDb);
    }

    /// <summary>
    /// Gets all the entity ids in this segment
    /// </summary>
    public unsafe EntityIds EntityIds()
    {
        var ids = GC.AllocateUninitializedArray<byte>(_rowCount * sizeof(EntityId) + sizeof(uint));
        {
            var span = ids.AsSpan();
            MemoryMarshal.Write(span, (uint)Count);
        }
        for (var i = 0; i < _rowCount; i++)
        {
            var prefix = new KeyPrefix(Uppers[i], Lowers[i]);
            var span = ids.AsSpan(i * sizeof(EntityId) + sizeof(uint));
            MemoryMarshal.Write(span, prefix.E);
        }

        return new EntityIds { Data = ids };
    }
    
    /// <summary>
    /// Enumerator.
    /// </summary>
    public struct Enumerator : IEnumerator<Datom>
    {
        private readonly IndexSegment _indexSegment;
        private int _index;

        internal Enumerator(IndexSegment indexSegment)
        {
            _indexSegment = indexSegment;
        }

        /// <inheritdoc/>
        public Datom Current { get; private set; } = default!;

        object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_index >= _indexSegment.Count) return false;
            Current = _indexSegment[_index++];
            return true;
        }

        /// <inheritdoc/>
        public void Reset() => _index = 0;

        /// <inheritdoc/>
        public void Dispose() { }
    }

    /// <summary>
    /// Returns the datoms in this segment as resolved datoms
    /// </summary>
    public IEnumerable<IReadDatom> Resolved(IConnection connection)
    {
        var resolver = connection.AttributeResolver;
        foreach (var datom in this)
        {
            yield return resolver.Resolve(datom);
        }
    }
}
