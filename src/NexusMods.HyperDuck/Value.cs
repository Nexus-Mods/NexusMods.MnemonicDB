using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace NexusMods.HyperDuck;

/// <summary>
/// A single db value, often using these is much slower than using the vector or chunk interfaces. None of the values
/// are implemented in a C# managed native format, so all the methods on this struct involve calling into DuckDB's
/// C++ code. 
/// </summary>
public unsafe partial struct Value : IDisposable
{
    private void* _ptr;

    internal Value(void* ptr)
    {
        _ptr = ptr;
    }

    [MustDisposeResource]
    public static Value Create(string value) => new(Native.duckdb_create_varchar(value));

    [MustDisposeResource]
    public static Value CreateFromUtf8(ReadOnlySpan<byte> data)
    {
        fixed (void* ptr = data)
        {
            return new Value(Native.duckdb_create_varchar_length(ptr, (ulong)data.Length));
        }
    }

    [MustDisposeResource]
    public static Value Create(bool value) => new(Native.duckdb_create_bool(value ? (byte)1 : (byte)0));

    [MustDisposeResource]
    public static Value Create(byte value) => new(Native.duckdb_create_uint8(value));

    [MustDisposeResource]
    public static Value Create(sbyte value) => new(Native.duckdb_create_int8(value));

    [MustDisposeResource]
    public static Value Create(short value) => new(Native.duckdb_create_int16(value));

    [MustDisposeResource]
    public static Value Create(ushort value) => new(Native.duckdb_create_uint16(value));

    [MustDisposeResource]
    public static Value Create(int value) => new(Native.duckdb_create_int32(value));

    [MustDisposeResource]
    public static Value Create(uint value) => new(Native.duckdb_create_uint32(value));

    [MustDisposeResource]
    public static Value Create(long value) => new(Native.duckdb_create_int64(value));

    [MustDisposeResource]
    public static Value Create(ulong value) => new(Native.duckdb_create_uint64(value));

    [MustDisposeResource]
    public static Value Create(float value) => new(Native.duckdb_create_float(value));

    [MustDisposeResource]
    public static Value Create(double value) => new(Native.duckdb_create_double(value));

    [MustDisposeResource]
    public static Value CreateNull() => new(Native.duckdb_create_null_value());

    [MustDisposeResource]
    public static Value From<T>(T value)
    {
        return value switch
        {
            null => CreateNull(),
            bool v => Create(v),
            byte v => Create(v),
            sbyte v => Create(v),
            short v => Create(v),
            ushort v => Create(v),
            int v => Create(v),
            uint v => Create(v),
            long v => Create(v),
            ulong v => Create(v),
            float v => Create(v),
            double v => Create(v),
            string v => Create(v),
            _ => throw new ArgumentException("Unsupported type", nameof(value))
        };
    }


    public bool GetBool() => Native.duckdb_get_bool(_ptr) != 0;

    public byte GetByte() => Native.duckdb_get_uint8(_ptr);

    public sbyte GetSByte() => Native.duckdb_get_int8(_ptr);

    public short GetInt16() => Native.duckdb_get_int16(_ptr);

    public ushort GetUInt16() => Native.duckdb_get_uint16(_ptr);

    public int GetInt32() => Native.duckdb_get_int32(_ptr);

    public uint GetUInt32() => Native.duckdb_get_uint32(_ptr);

    public long GetInt64() => Native.duckdb_get_int64(_ptr);

    public ulong GetUInt64() => Native.duckdb_get_uint64(_ptr);

    public float GetFloat() => Native.duckdb_get_float(_ptr);

    public double GetDouble() => Native.duckdb_get_double(_ptr);

    public bool IsNull => Native.duckdb_is_null_value(_ptr) != 0;

    [MustUseReturnValue]
    public LogicalType GetValueType() => new(Native.duckdb_get_value_type(_ptr));

    /// <summary>
    /// Gets the C# value from this db value, or returns the default if the pointer is invalid
    /// or the db-level value is 'NULL'. Assumes the C# type passed in matches the db type.
    /// </summary>
    public T GetDefault<T>(T defaultValue)
    {
        if (_ptr == null || IsNull) 
            return defaultValue;

        return defaultValue switch
        {
            bool => (T)(object)GetBool(),
            byte => (T)(object)GetByte(),
            sbyte => (T)(object)GetSByte(),
            short => (T)(object)GetInt16(),
            ushort => (T)(object)GetUInt16(),
            int => (T)(object)GetInt32(),
            uint => (T)(object)GetUInt32(),
            long => (T)(object)GetInt64(),
            ulong => (T)(object)GetUInt64(),
            float => (T)(object)GetFloat(),
            double => (T)(object)GetDouble(),
            _ => throw new ArgumentOutOfRangeException(nameof(defaultValue), defaultValue, null)
        };

    }
    
    internal partial class Native
    {
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_destroy_value(ref void* value);
        
        [LibraryImport(InternalConsts.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_varchar(string str);

        [LibraryImport(InternalConsts.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_varchar_length(void* str, ulong length);

        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_bool(byte value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_uint8(byte value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_int8(sbyte value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_int16(short value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_uint16(ushort value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_int32(int value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_uint32(uint value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_int64(long value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_uint64(ulong value);

        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_float(float value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_double(double value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial byte duckdb_get_bool(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial sbyte duckdb_get_int8(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial byte duckdb_get_uint8(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial short duckdb_get_int16(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial ushort duckdb_get_uint16(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int duckdb_get_int32(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial uint duckdb_get_uint32(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial long duckdb_get_int64(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial ulong duckdb_get_uint64(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial float duckdb_get_float(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial double duckdb_get_double(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_get_value_type(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial byte duckdb_is_null_value(void* value);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_null_value();
        
    }

    public void Dispose()
    {
        if (_ptr != null)
            return;
        Native.duckdb_destroy_value(ref _ptr);
        _ptr = null;
    }

}
