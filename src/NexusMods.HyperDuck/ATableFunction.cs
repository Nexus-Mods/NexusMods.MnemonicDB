using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace NexusMods.HyperDuck;

public abstract unsafe partial class ATableFunction
{
    protected ATableFunction()
    {
    }

    public void Register(Connection connection)
    {
        using var data = new RegistrationInfo(Native.duckdb_create_table_function());
        Setup(data);
        var handle = GCHandle.Alloc(this);
        Native.duckdb_table_function_set_extra_info(data._ptr, (void*)GCHandle.ToIntPtr(handle), &DeleteHandleFn);
        Native.duckdb_table_function_set_bind(data._ptr, &BindFn);
        Native.duckdb_table_function_set_init(data._ptr, &InitFn);
        Native.duckdb_table_function_set_local_init(data._ptr, &LocalInitFn);
        Native.duckdb_table_function_set_function(data._ptr, &Fn);
        
        if (Native.duckdb_register_table_function(connection._ptr, data._ptr) != State.Success)
            throw new InvalidOperationException("Failed to register table function.");
    }

    protected ref struct RegistrationInfo : IDisposable
    {
        internal void* _ptr;
        public RegistrationInfo(void* ptr)
        {
            _ptr = ptr;
        }
        
        public void SetName(string name)
        {
            Native.duckdb_table_function_set_name(_ptr, name);
        }

        public void AddParameter<T>()
        {
            using var logicalType = LogicalType.From<T>();
            Native.duckdb_table_function_add_parameter(_ptr, logicalType._ptr);
        }
        
        public void AddNamedParameter<T>(string name)
        {
            using var logicalType = LogicalType.From<T>();
            Native.duckdb_table_function_add_named_parameter(_ptr, name, logicalType._ptr);
        }
        
        public void Dispose()
        {
            Native.duckdb_destroy_table_function(ref _ptr);
        }
        
    }
    
    protected abstract void Setup(RegistrationInfo info);

    internal static partial class Native
    {
        #region Creation
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_create_table_function();
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_destroy_table_function(ref void* ptr);
        
        [LibraryImport(InternalConsts.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_name(void* ptr, string name);
  
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial State duckdb_register_table_function(void* conn, void* fn);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_function(void* ptr, delegate* unmanaged[Cdecl]<void*, void*, void> fnPtr);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_extra_info(void* ptr, void* extraInfo, delegate* unmanaged[Cdecl]<void*, void> deleteFn);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_bind(void* ptr, delegate* unmanaged[Cdecl]<void*, void> bindFn);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_init(void* ptr, delegate* unmanaged[Cdecl]<void*, void> initFn);

        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_local_init(void* ptr, delegate* unmanaged[Cdecl]<void*, void> localInitFn);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_supports_projection_pushdown(void* ptr, byte supportsProjectionPushdown);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_add_parameter(void* ptr, void* logicalType);
        
        [LibraryImport(InternalConsts.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_add_named_parameter(void* ptr, string name, void* logicalType);
        #endregion


        #region Bind
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_bind_get_extra_info(void* bindInfo);
        
        [LibraryImport(InternalConsts.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_bind_set_error(void* bindInfo, string error);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_bind_set_bind_data(void* bindInfo, void* data, delegate* unmanaged[Cdecl]<void*, void> deleteFn);
        
        [LibraryImport(InternalConsts.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void  duckdb_bind_add_result_column(void* bindInfo, string name, void* logicalType); 
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_bind_set_cardinality(void* bindInfo, ulong cardinality, byte isExact);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial ulong duckdb_bind_get_parameter_count(void* bindInfo);
        
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_bind_get_parameter(void* bindInfo, ulong index);
        
        [LibraryImport(InternalConsts.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_bind_get_named_parameter(void* bindInfo, string name);
        
        
        #endregion
        
        
        // Function
        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_function_get_extra_info(void* functionInfo);

        [LibraryImport(InternalConsts.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_function_get_bind_data(void* functionInfo);

        [LibraryImport(InternalConsts.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_function_set_error(void* functionInfo, string error);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Fn(void* functionInfo, void* chunk)
    {
        try
        {
            var handle = GCHandle.FromIntPtr((IntPtr)Native.duckdb_function_get_extra_info(functionInfo));
            if (handle.Target is not ATableFunction func)
                throw new InvalidOperationException("Invalid function pointer");

            var info = new FunctionInfo(functionInfo, chunk);
            func.Execute(info);
        }
        catch (Exception e)
        {
            Native.duckdb_function_set_error(functionInfo, e.Message);
        }
    }

    protected abstract void Execute(FunctionInfo functionInfo);

    public ref struct FunctionInfo
    {
        private void* _ptr;
        private void* _chunk;
        
        public FunctionInfo(void* ptr, void* chunk)
        {
            _ptr = ptr;
            _chunk = chunk;
        }
        
        public WritableChunk Chunk => new WritableChunk(_chunk);

        public T GetBindInfo<T>()
        {
            var info = GCHandle.FromIntPtr((IntPtr)Native.duckdb_function_get_bind_data(_ptr));
            if (info.Target is not T bindInfo)
                throw new InvalidOperationException("Invalid bind info pointer");
            return bindInfo;
        }
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void BindFn(void* bindInfo)
    {
        try
        {
            var extraInfo = Native.duckdb_bind_get_extra_info(bindInfo);
            var handle = GCHandle.FromIntPtr((IntPtr)extraInfo);
            if (handle.Target is not ATableFunction func)
                throw new InvalidOperationException("Invalid function pointer");

            var info = new BindInfo(bindInfo);
            func.Bind(info);
        }
        catch (Exception e)
        {
            Native.duckdb_bind_set_error(bindInfo, e.Message);
        }
    }

    protected abstract void Bind(BindInfo info);

    public ref struct BindInfo
    {
        private void* _ptr;
        public BindInfo(void* ptr)
        {
            _ptr = ptr;
        }
        
        /// <summary>
        /// Gets the number of regular (non-named) parameters to the function
        /// </summary>
        public ulong ParameterCount => Native.duckdb_bind_get_parameter_count(_ptr);
        
        [MustDisposeResource]
        public Value GetParameter(ulong index) => new(Native.duckdb_bind_get_parameter(_ptr, index));
        
        [MustDisposeResource]
        public Value GetParameter(string name) => new(Native.duckdb_bind_get_named_parameter(_ptr, name));

        public void SetBindInfo<T>(T bindInfo)
        {
            var handle = GCHandle.Alloc(bindInfo);
            Native.duckdb_bind_set_bind_data(_ptr, (void*)GCHandle.ToIntPtr(handle), &DeleteHandleFn);
        }
        
        public void AddColumn<T>(string name)
        {
            using var logicalType = LogicalType.From<T>();
            Native.duckdb_bind_add_result_column(_ptr, name, logicalType._ptr);
        }

        public void AddColumn(string name, LogicalType logicalType)
        {
            Native.duckdb_bind_add_result_column(_ptr, name, logicalType._ptr);
        }
        
        /// <summary>
        /// Estimate the number of rows returned by the tablescan, isExact should be set to false
        /// if the value is an approximation
        /// </summary>
        public void SetCardinality(ulong cardinality, bool isExact)
        {
            Native.duckdb_bind_set_cardinality(_ptr, cardinality, isExact ? (byte)1 : (byte)0);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void DeleteHandleFn(void* functionInfo)
    {
        var handle = GCHandle.FromIntPtr((IntPtr)functionInfo);
        handle.Free();
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void InitFn(void* initInfo)
    {
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void LocalInitFn(void* localInitInfo)
    {
    }
}
