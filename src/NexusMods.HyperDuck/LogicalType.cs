using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace NexusMods.HyperDuck;

public unsafe partial struct LogicalType : IDisposable
{
    internal void* _ptr;
    
    internal LogicalType(void* ptr)
    {
        _ptr = ptr;
    }

    [MustDisposeResource]
    public static LogicalType Create(DuckDbType type)
    {
        return new LogicalType(Native.duckdb_create_logical_type(type));
    }

    [MustDisposeResource]
    public static LogicalType From<T>()
    {
        if (typeof(T) == typeof(bool)) return Create(DuckDbType.Boolean);
        if (typeof(T) == typeof(byte)) return Create(DuckDbType.TinyInt);
        if (typeof(T) == typeof(short)) return Create(DuckDbType.SmallInt);
        if (typeof(T) == typeof(int)) return Create(DuckDbType.Integer);
        if (typeof(T) == typeof(long)) return Create(DuckDbType.BigInt);
        if (typeof(T) == typeof(sbyte)) return Create(DuckDbType.UTinyInt);
        if (typeof(T) == typeof(ushort)) return Create(DuckDbType.USmallInt);
        if (typeof(T) == typeof(uint)) return Create(DuckDbType.UInteger);
        if (typeof(T) == typeof(ulong)) return Create(DuckDbType.UBigInt);
        if (typeof(T) == typeof(string)) return Create(DuckDbType.Varchar);
        if (typeof(T) == typeof(ReadOnlySpan<char>)) return Create(DuckDbType.Varchar);
        if (typeof(T) == typeof(ReadOnlySpan<byte>)) return Create(DuckDbType.Blob);
        if (typeof(T) == typeof(float)) return Create(DuckDbType.Float);
        if (typeof(T) == typeof(double)) return Create(DuckDbType.Double);
        throw new NotImplementedException();
    }
    
    public int SizeInVector => TypeId switch
    {
        DuckDbType.TinyInt => 1,
        DuckDbType.SmallInt => 2,
        DuckDbType.Integer => 4,
        DuckDbType.BigInt => 8,
        DuckDbType.UTinyInt => 1,
        DuckDbType.USmallInt => 2,
        DuckDbType.UInteger => 4,
        DuckDbType.UBigInt => 8,
        DuckDbType.Float => 4,
        DuckDbType.Double => 8,
        DuckDbType.Varchar => StringElement.ElementSize, // This is only the size of the StringElement (duckdb_string_t), not the actual string data.
        DuckDbType.List => sizeof(ListEntry),
        DuckDbType.Enum => Native.duckdb_enum_internal_type(_ptr) switch
        {
            DuckDbType.UTinyInt => 1,
            DuckDbType.USmallInt => 2,
            DuckDbType.UInteger => 4,
            DuckDbType.UBigInt => 8,
            _ => throw new NotImplementedException("SizeInVector not implemented for enum type.")
        },
        DuckDbType.Union => 0,
        _ => throw new NotImplementedException("SizeInVector not implemented for type: " + TypeId)
    };

    [MustDisposeResource]
    public static LogicalType CreateListOf(LogicalType childType)
    {
        ArgumentNullException.ThrowIfNull(childType._ptr);
        return new LogicalType(Native.duckdb_create_list_type(childType._ptr));
    }

    [MustDisposeResource]
    public static LogicalType CreateEnum(params string[] values)
    {
        return new LogicalType(Native.duckdb_create_enum_type(values, (ulong)values.Length));
    }

    [MustDisposeResource]
    public static unsafe LogicalType CreateUnion(string[] names, ReadOnlySpan<LogicalType> types)
    {
        if (names.Length != types.Length)
            throw new ArgumentException("Names and types must be the same length.");

        Span<IntPtr> strs = stackalloc IntPtr[names.Length];
        
        for (var i = 0; i < names.Length; i++)
            strs[i] = Marshal.StringToHGlobalAnsi(names[i]);

        try
        {
            fixed (void* namesPtr = strs)
            {
                fixed (void* ptr = types)
                {
                    return new LogicalType(Native.duckdb_create_union_type(ptr, namesPtr, (ulong)names.Length));
                }
            }
        }
        finally
        {
            foreach (var str in strs)
                Marshal.FreeHGlobal(str);
        }
    }
    
    [MustDisposeResource]
    public static LogicalType CreateStruct(string[] names, ReadOnlySpan<LogicalType> types)
    {
        if (names.Length != types.Length)
            throw new ArgumentException("Names and types must be the same length.");

        fixed (void* ptr = types)
        {
            return new LogicalType(Native.duckdb_create_struct_type(ptr, names, (ulong)names.Length));
        }
    }
    
    public DuckDbType TypeId
    {
        get
        {
            ObjectDisposedException.ThrowIf(_ptr == null, nameof(LogicalType));
            return Native.duckdb_get_type_id(_ptr);
        }
    }

    public readonly LogicalType ListChildType()
    {
        ObjectDisposedException.ThrowIf(_ptr == null, nameof(LogicalType));
        if (TypeId != DuckDbType.List)
            throw new InvalidOperationException("Cannot get child type of non-list type.");
        return new LogicalType(Native.duckdb_list_type_child_type(_ptr));
    }

    private static partial class Native
    {
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void duckdb_destroy_logical_type(ref void* logical);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial DuckDbType duckdb_get_type_id(void* logical);

        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_list_type_child_type(void* logical);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_logical_type(DuckDbType typeId);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_list_type(void* childType);

        [LibraryImport(InternalConsts.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_enum_type(string[] values, ulong count);
        
        [LibraryImport(InternalConsts.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_struct_type(void* types, string[] values, ulong count);
        
        [LibraryImport(InternalConsts.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void* duckdb_create_union_type(void* types, void* values, ulong count);

        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial DuckDbType duckdb_enum_internal_type(void* type);
    }


    public void Dispose()
    {
        if (_ptr == null) return;
        Native.duckdb_destroy_logical_type(ref _ptr);
        _ptr = null;
    }
}
