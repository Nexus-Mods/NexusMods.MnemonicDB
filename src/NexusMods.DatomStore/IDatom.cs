using System;
using System.Buffers.Binary;
using static System.Text.Encoding;

namespace NexusMods.DatomStore;

public abstract class ADatom : IEquatable<ADatom>
{
    /// <summary>
    /// The entity id for this datom
    /// </summary>
    public abstract ulong Entity { get; }

    /// <summary>
    /// The attribute id for this datom
    /// </summary>
    public abstract ulong Attribute { get; }

    /// <summary>
    /// The value type for this datom
    /// </summary>
    public abstract ValueTypes ValueType { get; }

    /// <summary>
    /// The value span for this datom
    /// </summary>
    public abstract ReadOnlySpan<byte> ValueSpan { get; }

    /// <summary>
    /// The transaction id for this datom
    /// </summary>
    public abstract ulong Tx { get; }

    public bool Equals(ADatom? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Entity != other.Entity) return false;
        if (Attribute != other.Attribute) return false;
        if (ValueType != other.ValueType) return false;
        return CommonComparators.Compare(ValueType, ValueSpan, other.ValueType, other.ValueSpan) == 0;
    }
}
