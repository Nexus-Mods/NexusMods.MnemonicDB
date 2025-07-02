// ReSharper disable InconsistentNaming

using System;

namespace NexusMods.MnemonicDB.Abstractions.ElementComparers;

/// <summary>
/// Value tags are used to determine the type of values. Most of the values
/// are primitives, but a few specific tuple types are supported, mostly for
/// use in the Nexus Mods app.
/// </summary>
public enum ValueTag : byte
{
    /// <summary>
    /// Null value, no data
    /// </summary>
    Null = 0,
    /// <summary>
    /// Unsigned 8-bit integer
    /// </summary>
    UInt8 = 1,
    /// <summary>
    /// Unsigned 16-bit integer
    /// </summary>
    UInt16 = 2,
    /// <summary>
    /// Unsigned 32-bit integer
    /// </summary>
    UInt32 = 3,
    /// <summary>
    /// Unsigned 64-bit integer
    /// </summary>
    UInt64 = 4,
    /// <summary>
    /// Unsigned 128-bit integer
    /// </summary>
    UInt128 = 5,
    /// <summary>
    /// Unsigned 16-bit integer
    /// </summary>
    Int16 = 6,
    /// <summary>
    /// Unsigned 32-bit integer
    /// </summary>
    Int32 = 7,
    /// <summary>
    /// Unsigned 64-bit integer
    /// </summary>
    Int64 = 8,
    /// <summary>
    /// Unsigned 128-bit integer
    /// </summary>
    Int128 = 9,
    /// <summary>
    /// 32-bit floating point number
    /// </summary>
    Float32 = 10,
    /// <summary>
    /// 64-bit floating point number (double)
    /// </summary>
    Float64 = 11,
    /// <summary>
    /// ASCII string, case-sensitive
    /// </summary>
    Ascii = 12,
    /// <summary>
    /// UTF-8 string, case-sensitive
    /// </summary>
    Utf8 = 13,
    /// <summary>
    /// UTF-8 string, case-insensitive
    /// </summary>
    Utf8Insensitive = 14,
    /// <summary>
    /// Inline binary data
    /// </summary>
    Blob = 15,

    /// <summary>
    /// A blob sorted by its xxHash64 hash, and where the data is possibly stored in a separate location
    /// as to degrade the performance of the key storage
    /// </summary>
    HashedBlob = 16,

    /// <summary>
    /// A reference to another entity
    /// </summary>
    Reference = 17,
    
    /// <summary>
    /// A tuple of three values: a reference, an unsigned 16-bit integer, and a UTF-8 (case insensitive) string
    /// </summary>
    Tuple3_Ref_UShort_Utf8I = 64,
    
    /// <summary>
    /// A tuple of two values: an unsigned 16-bit integer and a UTF-8 (case insensitive) string
    /// </summary>
    Tuple2_UShort_Utf8I = 65,
}

public static class ValueTagExtensions
{
    public static Type ToClrType(this ValueTag tag) => tag switch
    {
        ValueTag.Null => typeof(bool),
        ValueTag.UInt8 => typeof(byte),
        ValueTag.UInt16 => typeof(ushort),
        ValueTag.UInt32 => typeof(uint),
        ValueTag.UInt64 => typeof(ulong),
        ValueTag.UInt128 => typeof(ulong),
        ValueTag.Int16 => typeof(short),
        ValueTag.Int32 => typeof(int),
        ValueTag.Int64 => typeof(long),
        ValueTag.Int128 => typeof(long),
        ValueTag.Float32 => typeof(float),
        ValueTag.Float64 => typeof(double),
        ValueTag.Ascii => typeof(string),
        ValueTag.Utf8 => typeof(string),
        ValueTag.Utf8Insensitive => typeof(string),
        ValueTag.Blob => typeof(byte[]),
        ValueTag.HashedBlob => typeof(byte[]),
        ValueTag.Reference => typeof(ulong),
        ValueTag.Tuple3_Ref_UShort_Utf8I => typeof(Tuple<EntityId, ushort, string>),
        ValueTag.Tuple2_UShort_Utf8I => typeof(Tuple<ushort, string>),
        _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
    };
}
