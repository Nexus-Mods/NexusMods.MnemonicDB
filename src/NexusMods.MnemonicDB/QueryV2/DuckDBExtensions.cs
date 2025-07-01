using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using DuckDB.NET.Native;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.QueryV2;

public static class DuckDBExtensions
{
    public static ulong VectorSize { get; } = NativeMethods.Helpers.DuckDBVectorSize();
    public static Type ToClrType(this DuckDBType type)
    {
        return type switch
        {
            DuckDBType.Invalid => throw new ArgumentException("Invalid DuckDB type"),
            DuckDBType.UnsignedBigInt => typeof(ulong),
            DuckDBType.Integer => typeof(int),
            DuckDBType.Varchar => typeof(string),
            _ => throw new NotSupportedException($"DuckDB type {type} is not supported")
        };
    }

    public static readonly FrozenDictionary<Type, DuckDBType> DuckDBTypeMap = new Dictionary<Type, DuckDBType>
    {
        { typeof(bool), DuckDBType.Boolean },
        { typeof(byte), DuckDBType.UnsignedTinyInt },
        { typeof(ushort), DuckDBType.UnsignedSmallInt },
        { typeof(uint), DuckDBType.UnsignedInteger },
        { typeof(ulong), DuckDBType.UnsignedBigInt },
        { typeof(UInt128), DuckDBType.UnsignedHugeInt },
        { typeof(short), DuckDBType.SmallInt },
        { typeof(int), DuckDBType.Integer },
        { typeof(long), DuckDBType.BigInt },
        { typeof(Int128), DuckDBType.HugeInt },
        { typeof(float), DuckDBType.Float },
        { typeof(double), DuckDBType.Double },
        { typeof(byte[]), DuckDBType.Blob },
        { typeof(string), DuckDBType.Varchar },
        { typeof(ValueTuple<ushort, string>), DuckDBType.Struct }, // Tuple2_UShort_Utf8I
        { typeof(ValueTuple<ulong, ushort, string>), DuckDBType.Struct }, // Tuple3_Ref_UShort_Utf8I
    }.ToFrozenDictionary();
    
    public static DuckDBLogicalType ToLogicalType(this Type type)
    {
        if (DuckDBTypeMap.TryGetValue(type, out var duckDbType))
        {
            if (type == typeof(ValueTuple<ulong, ushort, string>))
            {
                using var type1 = typeof(ulong).ToLogicalType();
                using var type2 = typeof(ushort).ToLogicalType();
                using var type3 = typeof(string).ToLogicalType();
                return HighPerfBindings.DuckDBCreateStructType(
                    [type1, type2, type3],
                    ["Item1", "Item2", "Item3"], 3);
            }
            if (type == typeof(ValueTuple<ushort, string>))
            {
                using var type1 = typeof(ushort).ToLogicalType();
                using var type2 = typeof(string).ToLogicalType();
                return HighPerfBindings.DuckDBCreateStructType(
                    [type1, type2],
                    ["Item1", "Item2"], 2);
            }
            return HighPerfBindings.DuckDBCreateLogicalType(duckDbType);
        }
        else
        {
            throw new NotSupportedException($"Type {type} is not supported by DuckDB.");
        }
    }

    public static Type ToLowLevelType(this ValueTag valueTag)
    {
        return valueTag switch
        {
            ValueTag.Null => typeof(bool),
            ValueTag.UInt8 => typeof(byte),
            ValueTag.UInt16 => typeof(ushort),
            ValueTag.UInt32 => typeof(uint),
            ValueTag.UInt64 => typeof(ulong),
            ValueTag.UInt128 => typeof(UInt128),
            ValueTag.Int16 => typeof(short),
            ValueTag.Int32 => typeof(int),
            ValueTag.Int64 => typeof(long),
            ValueTag.Int128 => typeof(Int128),
            ValueTag.Float32 => typeof(float),
            ValueTag.Float64 => typeof(double),
            ValueTag.Utf8 => typeof(string),
            ValueTag.Utf8Insensitive => typeof(string),
            ValueTag.Blob => typeof(byte[]),
            ValueTag.HashedBlob => typeof(byte[]),
            ValueTag.Tuple2_UShort_Utf8I => typeof(ValueTuple<ushort, string>),
            ValueTag.Tuple3_Ref_UShort_Utf8I => typeof(ValueTuple<ulong, ushort, string>),
            ValueTag.Ascii => typeof(string),
            ValueTag.Reference => typeof(ulong),
            _ => throw new NotSupportedException($"ValueTag {valueTag} is not supported by DuckDB.")
        };
    }
    
    public static DuckDBType ToDuckDBType(this ValueTag valueTag)
    {
        return valueTag switch
        {
            ValueTag.Null => DuckDBType.Boolean,
            ValueTag.UInt8 => DuckDBType.UnsignedTinyInt,
            ValueTag.UInt16 => DuckDBType.UnsignedSmallInt,
            ValueTag.UInt32 => DuckDBType.UnsignedInteger,
            ValueTag.UInt64 => DuckDBType.UnsignedBigInt,
            ValueTag.UInt128 => DuckDBType.UnsignedHugeInt,
            ValueTag.Int16 => DuckDBType.SmallInt,
            ValueTag.Int32 => DuckDBType.Integer,
            ValueTag.Int64 => DuckDBType.BigInt,
            ValueTag.Int128 => DuckDBType.HugeInt,
            ValueTag.Float32 => DuckDBType.Float,
            ValueTag.Float64 => DuckDBType.Double,
            ValueTag.Utf8 => DuckDBType.Varchar,
            ValueTag.Utf8Insensitive => DuckDBType.Varchar,
            ValueTag.Blob => DuckDBType.Blob,
            ValueTag.HashedBlob => DuckDBType.Blob,
            ValueTag.Tuple2_UShort_Utf8I => DuckDBType.Struct,
            ValueTag.Tuple3_Ref_UShort_Utf8I => DuckDBType.Struct,
            ValueTag.Ascii => DuckDBType.Varchar,
            ValueTag.Reference => DuckDBType.UnsignedBigInt,
            _ => throw new NotSupportedException($"ValueTag {valueTag} is not supported by DuckDB.")
        };
    }
}
