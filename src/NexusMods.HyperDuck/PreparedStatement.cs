using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dynamitey;
using JetBrains.Annotations;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Adaptor.Impls;

namespace NexusMods.HyperDuck;

public unsafe partial struct PreparedStatement : IDisposable
{
    public void* _ptr;
    private readonly Connection _connection;
    private readonly IRegistry _registry;
    private readonly ulong _parameterCount;
    private string[]? _names;

    public PreparedStatement(void* ptr, IRegistry registry, Connection connection)
    {
        _ptr = ptr;
        _connection = connection;
        _registry = registry;
        _parameterCount = Native.duckdb_nparams(_ptr);
    }

    public void Bind(object parameters)
    {
        if (parameters is Array arr)
        {
            for (var i = 0UL; i < (ulong)arr.Length; i++)
                // DuckDB Parameters are 1 indexed
                Bind(i + 1, arr.GetValue((int)i));
        }
        else if (_parameterCount == 1)
        {
            // DuckDB Parameters are 1 indexed
            Bind(1, parameters);
        }
        else
        {
            BindFrom(parameters);
        }
    }

    public void Bind<T>(ulong idx, T value)
    {
        var converter = _registry.GetBindingConverter<T>(value);
        converter.Bind(this, (int)idx, value);
    }

    public string[] GetParameterNames()
    {
        var count = Native.duckdb_nparams(_ptr);
        var paramNames = new string[count];
        for (ulong i = 0; i < count; i++)
        {
            // Names are 1 based indexes
            paramNames[i] = Native.duckdb_parameter_name(_ptr, i + 1);
        }
        return paramNames;
    }

    /// <summary>
    /// Used mostly just for marshalling into binding parameters
    /// </summary>
    public unsafe struct UInt128Struct
    {
        public ulong Low;
        public ulong High;
    }

    public void BindNative<T>(int idx, T value)
    {
        switch (value)
        {
            case bool b:
                Native.duckdb_bind_boolean(_ptr, idx, b);
                break;
            case byte b:
                Native.duckdb_bind_uint8(_ptr, idx, b);
                break;
            case ushort u:
                Native.duckdb_bind_uint16(_ptr, idx, u);
                break;
            case uint i:
                Native.duckdb_bind_uint32(_ptr, idx, i);
                break;
            case ulong l:
                Native.duckdb_bind_uint64(_ptr, idx, l);
                break;
            case sbyte sb:
                Native.duckdb_bind_int8(_ptr, idx, sb);
                break;
            case short s:
                Native.duckdb_bind_int16(_ptr, idx, s);
                break;
            case int i1:
                Native.duckdb_bind_int32(_ptr, idx, i1);
                break;
            case long l1:
                Native.duckdb_bind_int64(_ptr, idx, l1);
                break;
            case float f:
                Native.duckdb_bind_float(_ptr, idx, f);
                break;
            case double d:
                Native.duckdb_bind_double(_ptr, idx, d);
                break;
            case string s:
                Native.duckdb_bind_varchar(_ptr, idx, s);
                break;
            case UInt128 u128:
            {
                var dstStruct = new UInt128Struct();
                var src = new ReadOnlySpan<byte>(&u128, sizeof(UInt128));
                var dst = new Span<byte>(&dstStruct, sizeof(UInt128Struct));
                src.CopyTo(dst);
                Native.duckdb_bind_uhugeint(_ptr, idx, dstStruct);
                break;
            }
            case Int128 i128:
            {
                var dstStruct = new UInt128Struct();
                var src = new ReadOnlySpan<byte>(&i128, sizeof(UInt128));
                var dst = new Span<byte>(&dstStruct, sizeof(UInt128Struct));
                src.CopyTo(dst);
                Native.duckdb_bind_hugeint(_ptr, idx, dstStruct);
                break;
            }
            default:
                throw new ArgumentException("Unsupported type: " + typeof(T).Name);
        }
    }

    public static partial class Native
    {
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_destroy_prepare(ref void* stmt);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_execute_prepared(void* stmt, ref Result result);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_value(void* stmt, int param_idx, void* val);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_parameter_index(void* stmt, ref int param_idx_out, string name);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_boolean(void* stmt, int param_idx, [MarshalAs(UnmanagedType.I1)] bool val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_int8(void* stmt, int param_idx, sbyte val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_int16(void* stmt, int param_idx, short val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_int32(void* stmt, int param_idx, int val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_int64(void* stmt, int param_idx, long val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_hugeint(void* stmt, int param_idx, UInt128Struct val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_uhugeint(void* stmt, int param_idx, UInt128Struct val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_decimal(void* stmt, int param_idx, void* val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_uint8(void* stmt, int param_idx, byte val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_uint16(void* stmt, int param_idx, ushort val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_uint32(void* stmt, int param_idx, uint val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_uint64(void* stmt, int param_idx, ulong val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_float(void* stmt, int param_idx, float val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_double(void* stmt, int param_idx, double val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_date(void* stmt, int param_idx, void* val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_time(void* stmt, int param_idx, void* val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_timestamp(void* stmt, int param_idx, void* val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_timestamp_tz(void* stmt, int param_idx, void* val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_interval(void* stmt, int param_idx, void* val);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_varchar(void* stmt, int param_idx, string val);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_blob(void* stmt, int param_idx, void* data, int length);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial State duckdb_bind_null(void* stmt, int param_idx);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial ulong duckdb_nparams(void* stmt);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial string duckdb_parameter_name(void* stmt, ulong index);
    }

    [MustDisposeResource]
    public Result Execute()
    {
        Result resultPtr = new Result();
        if (Native.duckdb_execute_prepared(_ptr, ref resultPtr) != State.Success)
        {
            var error = Marshal.PtrToStringUTF8((nint)Connection.Native.duckdb_result_error(ref resultPtr));
            resultPtr.Dispose();
            throw new InvalidOperationException("Failed to execute prepared statement: " + error);
        }
        return resultPtr;
    }

    public void Dispose()
    {
        if (_ptr == null) 
            return;
        Native.duckdb_destroy_prepare(ref _ptr);
        _ptr = null;
    }

    public void BindFrom(object parameters)
    {
        CacheNames();
        for (ulong i = 0; i < _parameterCount; i++)
        {
            var obj = Dynamic.InvokeGet(parameters, _names![i]);
            Bind(i + 1, obj);
        }
    }

    private void CacheNames()
    {
        if (_names != null)
            return;
        _names = GetParameterNames();
    }
}
