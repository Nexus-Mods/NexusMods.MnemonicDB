using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A specialized index segment for entities, includes only A and V members for each datom
/// </summary>
public readonly struct EntitySegment : IEnumerable<Datom>
{
    private readonly EntityId _id;
    private readonly Memory<byte> _data;
    private readonly int _count;
    private readonly IDb _db;

    /// <summary>
    /// A specialized index segment for entities, includes only A and V members for each datom
    /// </summary>
    public EntitySegment(EntityId id, Memory<byte> data, int count, IDb db)
    {
        _id = id;
        _data = data;
        _count = count;
        _db = db;
    }

    /// <summary>
    /// Process the raw data from a IndexBuilder into a compressed EntitySegment
    /// </summary>
    internal static EntitySegment Create(IDb context, EntityId id, List<int> offsets, ReadOnlySpan<byte> data)
    {
        var rowCount = offsets.Count - 1;
        // Size of just the data.
        var dataSize = data.Length - (KeyPrefix.Size * rowCount);
        
        // Compressed size is only A, Valute Type, Offset and Data sizes
        var compressedSize = ((rowCount * sizeof(ushort)) + rowCount + (sizeof(uint) * rowCount)) + dataSize;
        
        var compressed = GC.AllocateUninitializedArray<byte>(compressedSize);
        var compressedSpan = compressed.AsSpan();
        
        var aSpan = compressedSpan.SliceFast(0, rowCount * sizeof(ushort)).CastFast<byte, AttributeId>();
        var vSpan = compressedSpan.SliceFast(rowCount * sizeof(ushort), rowCount).CastFast<byte, ValueTag>();
        var offsetSpan = compressedSpan.SliceFast(rowCount * sizeof(ushort) + rowCount, rowCount * sizeof(uint)).CastFast<byte, int>();
        var dataStart = rowCount * sizeof(ushort) + rowCount + (rowCount * sizeof(uint));
        var dataSpan = compressedSpan.SliceFast(dataStart, dataSize);

        var dataOffset = dataStart;
        for (var idx = 0; idx < rowCount; idx++)
        {
            var offsetStart = offsets[idx];
            var offsetEnd = offsets[idx + 1];
            var datomSpan = data.SliceFast(offsetStart, offsetEnd - offsetStart);
            var prefix = KeyPrefix.Read(datomSpan);
            var valueSpan = datomSpan.SliceFast(KeyPrefix.Size);

            aSpan[idx] = prefix.A;
            vSpan[idx] = prefix.ValueTag;
            offsetSpan[idx] = dataOffset;
            valueSpan.CopyTo(compressedSpan.SliceFast(dataOffset));
            dataOffset += valueSpan.Length;
        }
        return new EntitySegment(id, compressed, rowCount, context);
    }

    /// <summary>
    /// Get the value for the first occurrence of the given attribute id in the segment
    /// </summary>
    public bool TryGetValue<TAttribute, TValueType>(TAttribute attr, AttributeId id, out TValueType value) 
        where TAttribute : IReadableAttribute<TValueType>
    {
        var i = FirstOffsetOf(id);
        if (i == -1)
        {
            value = default!;
            return false;
        }
        return TryGetValue(attr, i, out value);
    }

    /// <summary>
    /// Get the value for the datom at the given index
    /// </summary>
    public bool TryGetValue<TAttribute, TValueType>(TAttribute attr, int i, out TValueType value)
        where TAttribute : IReadableAttribute<TValueType>
    {
        var valueTag = (ValueTag)_data.Span[(sizeof(ushort) * _count) + i];
        var offsetSpan = _data.Span.SliceFast((sizeof(ushort) * _count) + _count, _count * sizeof(uint))
            .CastFast<byte, int>();
        var offset = offsetSpan[i];
        ReadOnlySpan<byte> dataSpan;
        if (i == _count - 1)
        {
            dataSpan = _data.Span.SliceFast(offset);
        }
        else
        {
            var nextOffset = offsetSpan[i + 1];
            dataSpan = _data.Span.SliceFast(offset, nextOffset - offset);
        }

        value = attr.ReadValue(dataSpan, valueTag, _db.Connection.AttributeResolver);
        return true;
    }


    /// <summary>
    /// Returns the first occurrence of the given attribute id in the segment, or -1 if not found
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public int FirstOffsetOf(AttributeId id)
    {
        var attrIds = AttributeIds;
        for (var i = 0; i < _count; i++)
        {
            if (attrIds[i] == id)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Returns the last occurrence of the given attribute id in the segment after the given index, or start if not found
    /// </summary>
    public int LastOffsetOf(int start, AttributeId id)
    {
        var attrIds = AttributeIds;
        var i = start;
        for (; i < _count; i++)
        {
            if (attrIds[i] != id)
                return i;
        }
        return i;
    }
    
    /// <summary>
    /// Get a range for datoms of the given attribute id
    /// </summary>
    public Range GetRange(AttributeId id)
    {
        var start = FirstOffsetOf(id);
        if (start == -1)
            return new Range(0, 0);
        var end = LastOffsetOf(start, id);
        return new Range(start, end - start);
    }
    
    /// <summary>
    /// Get the attribute Ids in this segment, in datom order;
    /// </summary>
    public ReadOnlySpan<AttributeId> AttributeIds => _data.Span.SliceFast(0, _count * sizeof(ushort)).CastFast<byte, AttributeId>();

    /// <summary>
    /// The number of datoms in the segment
    /// </summary>
    public int Count => _count;
    
    public IEnumerator<Datom> GetEnumerator()
    {
        return _db.Datoms(SliceDescriptor.Create(_id)).GetEnumerator();
    }

    /// <summary>
    /// Returns true if the segment contains the given attribute
    /// </summary>
    public bool Contains(IAttribute attribute)
    {
        var aid = _db.Connection.AttributeCache.GetAttributeId(attribute.Id);
        return FirstOffsetOf(aid) != -1;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerable<IReadDatom> Resolved(IConnection connection)
    {
        var resolver = connection.AttributeResolver;
        foreach (var datom in this)
        {
            yield return resolver.Resolve(datom);
        }
    }
}
