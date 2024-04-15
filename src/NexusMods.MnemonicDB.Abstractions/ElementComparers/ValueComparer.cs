using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
///     Compares values and assumes that some previous comparator will guarantee that the values are of the same attribute.
/// </summary>
public class ValueComparer : IElementComparer
{
    /// <inheritdoc />
    public static int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return UnsafeCompare(a, b);
    }


    /// <summary>
    /// Safe compare that will sort values first by type and then by value.
    /// </summary>
    public static int SafeCompare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return a[0] != b[0] ? a[0].CompareTo(b[0]) : UnsafeCompare(a, b);
    }


    /// <summary>
    /// Compares two values, assuming that they are of the same attribute. This is the "unsafe" part of the name
    /// as the method will not complain if the values are of different attributes.
    /// </summary>
    public static int UnsafeCompare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var tagA = (ValueTags)a[0];
        Debug.Assert(tagA == (ValueTags)b[0], "Values are of different types");

        return tagA switch
        {
            ValueTags.Null => 0,
            ValueTags.UInt8 => ValueSerializer.GetUInt8(ref a).CompareTo(ValueSerializer.GetUInt8(ref b)),
            ValueTags.UInt16 => ValueSerializer.GetUInt16(ref a).CompareTo(ValueSerializer.GetUInt16(ref b)),
            ValueTags.UInt32 => ValueSerializer.GetUInt32(ref a).CompareTo(ValueSerializer.GetUInt32(ref b)),
            ValueTags.UInt64 => ValueSerializer.GetUInt64(ref a).CompareTo(ValueSerializer.GetUInt64(ref b)),
            ValueTags.UInt128 => ValueSerializer.GetUInt128(ref a).CompareTo(ValueSerializer.GetUInt128(ref b)),
            ValueTags.Int16 => ValueSerializer.GetInt16(ref a).CompareTo(ValueSerializer.GetInt16(ref b)),
            ValueTags.Int32 => ValueSerializer.GetInt32(ref a).CompareTo(ValueSerializer.GetInt32(ref b)),
            ValueTags.Int64 => ValueSerializer.GetInt64(ref a).CompareTo(ValueSerializer.GetInt64(ref b)),
            ValueTags.Int128 => ValueSerializer.GetInt128(ref a).CompareTo(ValueSerializer.GetInt128(ref b)),
            ValueTags.Float32 => ValueSerializer.GetFloat32(ref a).CompareTo(ValueSerializer.GetFloat32(ref b)),
            ValueTags.Float64 => ValueSerializer.GetFloat64(ref a).CompareTo(ValueSerializer.GetFloat64(ref b)),
            ValueTags.Ascii => ValueSerializer.GetRaw(ref a).SequenceCompareTo(ValueSerializer.GetRaw(ref b)),
            ValueTags.Utf8 => ValueSerializer.GetRaw(ref a).SequenceCompareTo(ValueSerializer.GetRaw(ref b)),
            ValueTags.Utf8Insensitive => string.Compare(ValueSerializer.GetUtf8(ref a), ValueSerializer.GetUtf8(ref b),
                StringComparison.OrdinalIgnoreCase),
            ValueTags.Blob => ValueSerializer.GetRaw(ref a).SequenceCompareTo(ValueSerializer.GetRaw(ref b)),
            _ => InvalidTagException(tagA)
        };
    }

    private static int InvalidTagException(ValueTags tag)
    {
        throw new ArgumentOutOfRangeException("Invalid tag: " + tag);
        return 0;
    }
}
