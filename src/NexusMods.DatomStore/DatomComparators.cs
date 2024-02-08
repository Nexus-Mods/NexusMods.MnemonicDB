using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static System.Buffers.Binary.BinaryPrimitives;
using static System.Text.Encoding;

namespace NexusMods.DatomStore;

internal enum Comparer
{
    EAV,
}

internal static class CommonComparators
{
    /// <summary>
    /// Makes a comparer for the specified ordering
    /// </summary>
    /// <param name="comparer"></param>
    /// <typeparam name="TType"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static ADatomComparer<TType> MakeComparer<TType>(Comparer comparer)
    where TType : notnull, ADatom
    {
        return comparer switch
        {
            Comparer.EAV => new EAVComparer<TType>(),
            _ => throw new ArgumentOutOfRangeException(nameof(comparer), comparer, null)
        };
    }

    internal static int Compare(ValueTypes valueType, ReadOnlySpan<byte> valueSpan, ValueTypes otherValueType, ReadOnlySpan<byte> otherValueSpan)
    {
        if (valueType != otherValueType)
            return (byte)valueType < (byte)otherValueType ? -1 : 1;
        return valueType switch
        {
            ValueTypes.Ulong => ReadUInt64BigEndian(valueSpan) < ReadUInt64BigEndian(otherValueSpan) ? -1 : 1,
            ValueTypes.Long => ReadInt64BigEndian(valueSpan) < ReadInt64BigEndian(otherValueSpan) ? -1 : 1,
            ValueTypes.Float => ReadSingleBigEndian(valueSpan) < ReadSingleBigEndian(otherValueSpan) ? -1 : 1,
            ValueTypes.Double => ReadSingleBigEndian(valueSpan) < ReadSingleBigEndian(otherValueSpan) ? -1 : 1,
            ValueTypes.Blob => valueSpan.SequenceCompareTo(otherValueSpan),
            ValueTypes.Reference => ReadUInt64BigEndian(valueSpan) < ReadUInt64BigEndian(otherValueSpan) ? -1 : 1,
            ValueTypes.String => string.Compare(UTF8.GetString(valueSpan), UTF8.GetString(otherValueSpan), StringComparison.InvariantCulture),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}


public abstract class ADatomComparer<TType> : IComparer<TType>
    where TType : notnull, ADatom
{
    public int Compare(TType? x, TType? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(null, x)) return -1;
        if (ReferenceEquals(null, y)) return 1;
        return CompareNotNull(x, y);
    }

    /// <summary>
    /// Shortcut for Compare when both x and y are not null and not the same reference.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public abstract int CompareNotNull([NotNull] TType x, [NotNull] TType y);

}

/// <summary>
/// Compares two datoms by their entity, attribute, value type and value span, ignoring the transaction and index.
/// </summary>
/// <typeparam name="TType"></typeparam>
public sealed class EAVComparer<TType> : ADatomComparer<TType>
    where TType : notnull, ADatom
{
    public override int CompareNotNull([NotNull] TType x, [NotNull] TType y)
    {
        if (x.Entity != y.Entity) return x.Entity < y.Entity ? -1 : 1;
        if (x.Attribute != y.Attribute) return x.Attribute < y.Attribute ? -1 : 1;
        if (x.ValueType != y.ValueType) return x.ValueType < y.ValueType ? -1 : 1;
        return CommonComparators.Compare(x.ValueType, x.ValueSpan, y.ValueType, y.ValueSpan);
    }
}
