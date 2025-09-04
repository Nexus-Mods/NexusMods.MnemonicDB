using System;

namespace NexusMods.HyperDuck;

public enum DuckDbType : ulong
{
	Invalid = 0,
	// bool
	Boolean = 1,
	// int8_t
	TinyInt = 2,
	// int16_t
	SmallInt = 3,
	// int32_t
	Integer = 4,
	// int64_t
	BigInt = 5,
	// uint8_t
	UTinyInt = 6,
	// uint16_t
	USmallInt = 7,
	// uint32_t
	UInteger = 8,
	// uint64_t
	UBigInt = 9,
	// float
	Float = 10,
	// double
	Double = 11,
	// duckdb_timestamp (microseconds)
	Timestamp = 12,
	// duckdb_date
	Date = 13,
	// duckdb_time
	Time = 14,
	// duckdb_interval
	Interval = 15,
	// duckdb_hugeint
	Hugeint = 16,
	// duckdb_uhugeint
	Uhugeint = 32,
	// const char*
	Varchar = 17,
	// duckdb_blob
	Blob = 18,
	// duckdb_decimal
	Decimal = 19,
	// duckdb_timestamp_s (seconds)
	TimestampS = 20,
	// duckdb_timestamp_ms (milliseconds)
	TimestampMs = 21,
	// duckdb_timestamp_ns (nanoseconds)
	TimestampNs = 22,
	// enum type, only useful as logical type
	Enum = 23,
	// list type, only useful as logical type
	List = 24,
	// struct type, only useful as logical type
	Struct = 25,
	// map type, only useful as logical type
	Map = 26,
	// duckdb_array, only useful as logical type
	Array = 33,
	// duckdb_hugeint
	Uuid = 27,
	// union type, only useful as logical type
	Union = 28,
	// duckdb_bit
	Bit = 29,
	// duckdb_time_tz
	TimeTz = 30,
	// duckdb_timestamp (microseconds)
	TimestampTz = 31,
	// enum type, only useful as logical type
	Any = 34,
	// duckdb_varint
	Varint = 35,
	// enum type, only useful as logical type
	Sqlnull = 36,
	// enum type, only useful as logical type
	StringLiteral = 37,
	// enum type, only useful as logical type
	IntegerLiteral = 38,
}


public static class DuckDbTypeExtensions
{
    public static Type ElementType(this DuckDbType type)
    {
        return type switch
        {
            DuckDbType.Boolean => typeof(bool),
            DuckDbType.TinyInt => typeof(sbyte),
            DuckDbType.SmallInt => typeof(short),
            DuckDbType.Integer => typeof(int),
            DuckDbType.BigInt => typeof(long),
            DuckDbType.Hugeint => typeof(Int128),
            DuckDbType.UTinyInt => typeof(byte),
            DuckDbType.USmallInt => typeof(ushort),
            DuckDbType.UInteger => typeof(uint),
            DuckDbType.UBigInt => typeof(ulong),
            DuckDbType.Uhugeint => typeof(UInt128),
            DuckDbType.Float => typeof(float),
            DuckDbType.Double => typeof(double),
            DuckDbType.Varchar => typeof(StringElement),
            _ => throw new NotSupportedException("Element type not supported for type: " + type)
        };
    }

    public static DuckDbType ToDuckDbEnum<T>()
    {
        if (typeof(T) == typeof(bool)) return DuckDbType.Boolean;
        if (typeof(T) == typeof(sbyte)) return DuckDbType.TinyInt;
        if (typeof(T) == typeof(short)) return DuckDbType.SmallInt;
        if (typeof(T) == typeof(int)) return DuckDbType.Integer;
        if (typeof(T) == typeof(long)) return DuckDbType.BigInt;
        if (typeof(T) == typeof(Int128)) return DuckDbType.Hugeint;
        if (typeof(T) == typeof(byte)) return DuckDbType.UTinyInt;
        if (typeof(T) == typeof(ushort)) return DuckDbType.USmallInt;
        if (typeof(T) == typeof(uint)) return DuckDbType.UInteger;
        if (typeof(T) == typeof(ulong)) return DuckDbType.UBigInt;
        if (typeof(T) == typeof(UInt128)) return DuckDbType.Uhugeint;
        if (typeof(T) == typeof(float)) return DuckDbType.Float;
        if (typeof(T) == typeof(double)) return DuckDbType.Double;
        if (typeof(T) == typeof(StringElement)) return DuckDbType.Varchar;
        throw new NotSupportedException("DuckDB type not supported for type: " + typeof(T));
    }
}
