using System;
using System.Collections.Generic;
using System.Text;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// A wrapper for a datom that acts like one would expect from a polymorphic key. That is to say it ignores the
/// TxId and only considers the EntityId, AttributeId, and Value. However for salar attribute it also ignores
/// the value and only considers the EntityId and AttributeId.
/// </summary>
public readonly struct DatomKey : IEqualityComparer<DatomKey>
{
    private readonly EntityId _eid;
    private readonly AttributeId _attributeId;
    private readonly object? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatomKey"/> struct.
    /// </summary>
    public DatomKey(EntityId eid, AttributeId attributeId, object? value)
    {
        _eid = eid;
        _attributeId = attributeId;
        _value = value;
    }
    
    /// <summary>
    /// The entity id of the datom.
    /// </summary>
    public EntityId E => _eid;
    
    /// <summary>
    /// The attribute of the datom.
    /// </summary>
    public AttributeId A => _attributeId;
    
    /// <inheritdoc />
    public bool Equals(DatomKey x, DatomKey y)
    {
        if (x._eid != y._eid)
            return false;

        if (x._attributeId != y._attributeId)
            return false;

        return Equals(x._value, y._value);
    }

    /// <inheritdoc />
    public int GetHashCode(DatomKey obj)
    {
        var hash = new HashCode();
        hash.Add(obj._eid);
        hash.Add(obj._attributeId);
        hash.Add(obj._value);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (_value == null)
            return $"({_eid}, {_attributeId})";
        return $"({_eid}, {_attributeId}, {_value})";
    }
    
    private static string ToHexString(ReadOnlyMemory<byte> memory)
    {
        var span = memory.Span;
        var sb = new StringBuilder(span.Length * 2);
        foreach (var b in span)
        {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }
}
