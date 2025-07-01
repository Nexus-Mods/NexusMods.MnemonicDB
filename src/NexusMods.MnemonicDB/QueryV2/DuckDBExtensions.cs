using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using DuckDB.NET.Native;

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
        { typeof(int), DuckDBType.Integer },
        { typeof(ulong), DuckDBType.UnsignedBigInt },
    }.ToFrozenDictionary();
    
    public static bool TryGetDuckDBType(Type type, out DuckDBType duckDbType)
    {
        if (DuckDBTypeMap.TryGetValue(type, out duckDbType))
        {
            return true;
        }

        duckDbType = DuckDBType.Invalid;
        return false;
    }

    public static DuckDBLogicalType ToLogicalType(this Type type)
    {
        if (DuckDBTypeMap.TryGetValue(type, out var duckDbType))
        {
            return HighPerfBindings.DuckDBCreateLogicalType(duckDbType);
        }
        else
        {
            throw new NotSupportedException($"Type {type} is not supported by DuckDB.");
        }
    }
}
