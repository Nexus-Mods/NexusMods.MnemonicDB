using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
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
