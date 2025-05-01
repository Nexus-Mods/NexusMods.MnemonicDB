using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// A specialized index segment for entities, includes only A and V members for each datom
/// </summary>
public readonly struct EntitySegment : IEnumerable<Datom>
{
    private readonly EntityId _id;
    private readonly AVSegment _data;
    private readonly IDb _db;

    /// <summary>
    /// A specialized index segment for entities, includes only A and V members for each datom
    /// </summary>
    public EntitySegment(EntityId id, AVSegment data, IDb db)
    {
        _id = id;
        _data = data;
        _db = db;
    }
    
    /// <summary>
    /// The Db associated with this segment
    /// </summary>
    public IDb Db => _db;
    

    /// <summary>
    /// Get the value for the first occurrence of the given attribute id in the segment
    /// </summary>
    public bool TryGetValue<TAttribute, TValueType>(TAttribute attr, AttributeId id, [NotNullWhen(true)] out TValueType value) 
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
        var offset = _data.GetOffsets()[i];
        var dataSpan = offset.GetSpan(_data);
        var valueTag = _data.GetValueTypes()[i];
        value = attr.ReadValue(dataSpan, valueTag, _db.Connection.AttributeResolver);
        return true;
    }

    /// <summary>
    /// Returns true if the value/attribute and entity represented by the datom is in this segment
    /// </summary>
    public unsafe bool Contains(in AVData avData)
    {
        var index = FirstOffsetOf(avData.A);
        if (index == -1)
            return false;

        fixed (byte* aPtr = avData.Value.Span)
        {
            for (var i = index; i < _data.GetCount(); i++)
            {
                if (avData.A != _data.GetAttributeIds()[i])
                    return false;
                
                var otherType = _data.GetValueTypes()[i];
                if (avData.ValueType != otherType)
                    return false;
                
                var valueSpan = _data.GetOffsets()[i].GetSpan(_data);
                fixed (byte* bPtr = valueSpan)
                {
                    var cmp = avData.ValueType.Compare(aPtr, avData.Value.Span.Length, bPtr, valueSpan.Length);
                    if (cmp == 0)
                        return true;
                }
            }
        }

        return false;
    }


    /// <summary>
    /// Returns the first occurrence of the given attribute id in the segment, or -1 if not found
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public int FirstOffsetOf(AttributeId id)
    {
        var attrIds = AttributeIds;
        return attrIds.IndexOf(id);
    }

    /// <summary>
    /// Returns the last occurrence of the given attribute id in the segment after the given index, or start if not found
    /// </summary>
    public int LastOffsetOf(int start, AttributeId id)
    {
        var found = AttributeIds.SliceFast(start).LastIndexOf(id);
        return found == -1 ? start : start + found + 1;
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
        return new Range(start, end);
    }

    /// <summary>
    /// Get the attribute Ids in this segment, in datom order;
    /// </summary>
    public ReadOnlySpan<AttributeId> AttributeIds => _data.GetAttributeIds();

    /// <summary>
    /// The number of datoms in the segment
    /// </summary>
    public int Count => _data.GetCount();
    

    /// <summary>
    /// Returns true if the segment contains the given attribute
    /// </summary>
    public bool Contains(IAttribute attribute)
    {
        var aid = _db.Connection.AttributeCache.GetAttributeId(attribute.Id);
        return FirstOffsetOf(aid) != -1;
    }

    /// <summary>
    /// Returns the enumerator.
    /// </summary>
    public IndexSegment.Enumerator GetEnumerator() => _db.Datoms(SliceDescriptor.Create(_id)).GetEnumerator();

    /// <inheritdoc />
    IEnumerator<Datom> IEnumerable<Datom>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<IReadDatom> Resolved(IConnection connection)
    {
        var resolver = connection.AttributeResolver;
        foreach (var datom in this)
        {
            yield return resolver.Resolve(datom);
        }
    }
    
    /// <summary>
    /// Returns a very light weight enumerable for the AV data in this segment. Unlike the normal enumerator,
    /// this doesn't return the datom nor re-query the database for the datom, but instead returns the attribute id,
    /// value type and the value span.
    /// </summary>
    public AVEnumerable GetAVEnumerable() => new(this);

    public struct AVEnumerable(EntitySegment segment)
    {
        public AVEnumerator GetEnumerator() => new(segment);
    }

    public ref struct AVEnumerator
    {
        private int _idx;
        private readonly EntitySegment _segment;
        private AVData _current;

        public AVEnumerator(EntitySegment segment)
        {
            _idx = -1;
            _segment = segment;
        }

        public bool MoveNext()
        {
            if (_idx + 1 >= _segment.Count)
                return false;
            _idx++;
            _current = new AVData(_segment._data.GetAttributeIds()[_idx], _segment._data.GetValueTypes()[_idx], _segment._data.GetOffsets()[_idx].GetMemory(_segment._data));
            return true;
        }

        public AVData Current => _current;
    }

    public readonly struct AVData
    {
        public AVData(AttributeId a, ValueTag valueType, ReadOnlyMemory<byte> valueSpan)
        {
            A = a;
            ValueType = valueType;
            Value = valueSpan;
        }
        
        public readonly AttributeId A;
        public readonly ValueTag ValueType;
        public readonly ReadOnlyMemory<byte> Value;
    }
}

