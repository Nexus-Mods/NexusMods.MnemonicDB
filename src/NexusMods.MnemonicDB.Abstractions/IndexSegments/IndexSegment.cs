using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.Paths;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
///  A segment of an index, used most often as a cache. For example when an entity is read from the database,
/// the whole entity may be cached in one of these segments for fast access.
/// </summary>
public readonly struct IndexSegment : IEnumerable<Datom>
{
    private readonly IAttributeRegistry _registry;
    private readonly int _rowCount;
    private readonly ReadOnlyMemory<byte> _data;


    /// <summary>
    /// Construct a new index segment from the given data and offsets
    /// </summary>
    public IndexSegment(ReadOnlySpan<byte> data, ReadOnlySpan<int> offsets, IAttributeRegistry registry)
    {
        _registry = registry;
        if (data.Length == 0)
        {
            _rowCount = 0;
            _data = ReadOnlyMemory<byte>.Empty;
            return;
        }
        _rowCount = offsets.Length - 1;
        var memory = new Memory<byte>(GC.AllocateUninitializedArray<byte>(data.Length + (_rowCount + 1) * sizeof(int)));
        _data = memory;
        ReprocessData(data, offsets, memory.Span);
    }

    /// <summary>
    /// All the upper values
    /// </summary>
    private ReadOnlySpan<ulong> _uppers => _data.Span.SliceFast(0, _rowCount * sizeof(ulong)).CastFast<byte, ulong>();

    /// <summary>
    /// All the lower values
    /// </summary>
    private ReadOnlySpan<ulong> _lowers => _data.Span.SliceFast(_rowCount * sizeof(ulong), _rowCount * sizeof(ulong)).CastFast<byte, ulong>();

    /// <summary>
    /// All the offsets
    /// </summary>
    private ReadOnlySpan<int> _offsets => _data.Span.SliceFast(_rowCount * sizeof(ulong) * 2, (_rowCount + 1) * sizeof(int)).CastFast<byte, int>();

    /// <summary>
    /// Pivots all the data into 4 columns:
    ///  - (ulong) upper part of the key prefix
    ///  - (ulong) lower part of the key prefix
    ///  - (int) offsets for each row's value into the value blob
    ///  - (byte[]) value blob
    /// </summary>
    private void ReprocessData(ReadOnlySpan<byte> data, ReadOnlySpan<int> offsets, Span<byte> dataSpan)
    {
        var uppers = dataSpan.SliceFast(0, _rowCount * sizeof(ulong)).CastFast<byte, ulong>();
        var lowers = dataSpan.SliceFast(_rowCount * sizeof(ulong), _rowCount * sizeof(ulong)).CastFast<byte, ulong>();

        // Extra space for one int in the offsets so we can calculate the size of the last row
        var valueOffsets = dataSpan.SliceFast(_rowCount * sizeof(ulong) * 2, (_rowCount + 1) * sizeof(int)).CastFast<byte, int>();
        var values = dataSpan.SliceFast((_rowCount * (sizeof(ulong) * 2 + sizeof(int))) + sizeof(int));

        var relativeValueOffset = 0;

        // The first row starts at the beginning of the value blob
        var absoluteValueOffset = _rowCount * (sizeof(ulong) * 2 + sizeof(int)) + sizeof(int);

        for (var i = 0; i < _rowCount; i++)
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
        valueOffsets[_rowCount] = absoluteValueOffset;
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
    /// The assigned registry id
    /// </summary>
    public RegistryId RegistryId => _registry.Id;

    /// <summary>
    /// Get the datom of the given index
    /// </summary>
    public Datom this[int idx]
    {
        get
        {
            var offsets = _offsets;
            var fromOffset = offsets[idx];
            var toOffset = offsets[idx + 1];

            var valueSlice = _data.Slice(fromOffset, toOffset - fromOffset);

            return new Datom(new KeyPrefix(_uppers[idx], _lowers[idx]), valueSlice, _registry);
        }
    }

    /// <summary>
    /// Returns true if the segment contains the given attribute
    /// </summary>
    public bool Contains(IAttribute attribute)
    {
        var id = attribute.GetDbId(_registry.Id);
        foreach (var datom in this)
            if (datom.A == id)
                return true;
        return false;
    }

    /// <inheritdoc />
    public IEnumerator<Datom> GetEnumerator()
    {
        for (var i = 0; i < _rowCount; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Create a new index segment from the given datoms
    /// </summary>
    public static IndexSegment From(IAttributeRegistry registry, IReadOnlyCollection<Datom> datoms)
    {
        using var builder = new IndexSegmentBuilder(registry, datoms.Count);
        builder.Add(datoms);
        return builder.Build();
    }

    /// <summary>
    /// Finds the first index of the given entity id
    /// </summary>
    /// <returns></returns>
    public int FindFirst(EntityId find)
    {
        var left = 0;
        var right = _rowCount - 1;
        var result = -1;
        while (left <= right)
        {
            var mid = left + (right - left) / 2;


            var lower = _lowers[mid];
            var e = EntityId.From((lower & 0xFF00000000000000) | ((lower >> 8) & 0x0000FFFFFFFFFFFF));

            var comparison = e.CompareTo(find);
            if (comparison == 0)
            {
                result = mid; // Don't return, but continue searching to the left
                right = mid - 1;
            }
            else if (comparison < 0)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }
        return result; // Return the first occurrence found, or -1 if not found
    }

    public int FindFirstAVX2(ulong find)
    {
        var targetVector = Vector256<ulong>.Zero.WithElement(0, find)
            .WithElement(1, find)
            .WithElement(2, find)
            .WithElement(3, find);

        var casted = MemoryMarshal.Cast<ulong, Vector256<ulong>>(_lowers);

        for (var idx = 0; idx < casted.Length; idx += 1)
        {
            var lowers = casted[idx];

            var maskHigh = Avx2.And(lowers, Vector256.Create(0x7F00000000000000UL));
            var maskLow = Avx2.And(Avx2.ShiftRightLogical(lowers, 8), Vector256.Create(0x0000FFFFFFFFFFFFUL));
            var ored = Avx2.Or(maskHigh, maskLow);

            var comparison = Avx2.CompareEqual(ored, targetVector);

            var mask = Avx2.MoveMask(comparison.As<ulong, byte>());
            if (mask != 0)
            {
                var index = BitOperations.TrailingZeroCount(mask) / 8;
                return idx * Vector256<ulong>.Count + index;
            }
        }

        // Handle remaining elements
        for (int i = (casted.Length * Vector256<ulong>.Count); i < _lowers.Length; i++)
        {
            if (_lowers[i] >= find)
            {
                return i;
            }
        }

        return -1; // No value found that is greater than or equal to the target
    }
}
