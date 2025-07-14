// ReSharper disable InconsistentNaming

using System;
using NexusMods.HyperDuck;
// ReSharper disable NotDisposedResource

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

public static class ValueTagsExtensions
{
    private static readonly LogicalType[] _duckDbTypes;

    static ValueTagsExtensions()
    {
        _duckDbTypes = new LogicalType[(int)(ValueTag.Tuple2_UShort_Utf8I + 1)];
        _duckDbTypes[(int)ValueTag.Null] = LogicalType.From<bool>();
        _duckDbTypes[(int)ValueTag.UInt8] = LogicalType.From<byte>();
        _duckDbTypes[(int)ValueTag.UInt16] = LogicalType.From<ushort>();
        _duckDbTypes[(int)ValueTag.UInt32] = LogicalType.From<uint>();
        _duckDbTypes[(int)ValueTag.UInt64] = LogicalType.From<ulong>();
        _duckDbTypes[(int)ValueTag.Int16] = LogicalType.From<short>();
        _duckDbTypes[(int)ValueTag.Int32] = LogicalType.From<int>();
        _duckDbTypes[(int)ValueTag.Int64] = LogicalType.From<long>();
        _duckDbTypes[(int)ValueTag.Float32] = LogicalType.From<float>();
        _duckDbTypes[(int)ValueTag.Float64] = LogicalType.From<double>();
        _duckDbTypes[(int)ValueTag.Ascii] = LogicalType.From<string>();
        _duckDbTypes[(int)ValueTag.Utf8] = LogicalType.From<string>();
        _duckDbTypes[(int)ValueTag.Utf8Insensitive] = LogicalType.From<string>();
        _duckDbTypes[(int)ValueTag.Blob] = LogicalType.From<byte[]>();
        _duckDbTypes[(int)ValueTag.HashedBlob] = LogicalType.From<byte[]>();
        _duckDbTypes[(int)ValueTag.Reference] = LogicalType.From<ulong>();
        _duckDbTypes[(int)ValueTag.Tuple3_Ref_UShort_Utf8I] = LogicalType.CreateStruct(["Item1", "Item2", "Item3"],
            [LogicalType.From<ulong>(), LogicalType.From<ushort>(), LogicalType.From<string>()]);
        _duckDbTypes[(int)ValueTag.Tuple2_UShort_Utf8I] = LogicalType.CreateStruct(["Item1", "Item2"], 
            [LogicalType.From<ushort>(), LogicalType.From<string>()]);
    }

    public static LogicalType DuckDbType(this ValueTag tag)
    {
        return _duckDbTypes[(int)tag];
    }
}
