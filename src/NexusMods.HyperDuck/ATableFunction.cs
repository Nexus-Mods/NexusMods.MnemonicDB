using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using JetBrains.Annotations;

namespace NexusMods.HyperDuck;

public abstract unsafe partial class ATableFunction
{
    private ulong _revision;

    protected ATableFunction()
    {
        _revision = 0;
    }

    public string Name { get; set; } = "";
    public ulong Revision => _revision;

    protected void Revise()
    {
        Interlocked.Increment(ref _revision);
    }

    public void Register(Connection connection)
    {
        using var data = new RegistrationInfo(Native.duckdb_create_table_function(), this);
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
        private readonly ATableFunction _fn;

        public RegistrationInfo(void* ptr, ATableFunction fn)
        {
            _ptr = ptr;
            _fn = fn;
        }
        
        public void SetName(string name)
        {
            name = name.ToUpper();
            _fn.Name = name;
            Native.duckdb_table_function_set_name(_ptr, name);
        }

        public void AddParameter(LogicalType logicalType)
        {
            Native.duckdb_table_function_add_parameter(_ptr, logicalType._ptr);
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
            //Native.duckdb_destroy_table_function(ref _ptr);
        }

        /// <summary>
        /// Marks this function as support predicate pushdown. When the function is bound, be sure
        /// to check which columns are requested by the engine, as some may not be required and the
        /// engine will likely request them out-of-order.
        /// </summary>
        public void SupportsPredicatePushdown()
        {
            Native.duckdb_table_function_supports_projection_pushdown(_ptr, 1);
        }
    }
    
    protected abstract void Setup(RegistrationInfo info);

    internal static partial class Native
    {
        #region Creation
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_create_table_function();
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_destroy_table_function(ref void* ptr);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_name(void* ptr, string name);
  
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial State duckdb_register_table_function(void* conn, void* fn);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_function(void* ptr, delegate* unmanaged[Cdecl]<void*, void*, void> fnPtr);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_extra_info(void* ptr, void* extraInfo, delegate* unmanaged[Cdecl]<void*, void> deleteFn);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_bind(void* ptr, delegate* unmanaged[Cdecl]<void*, void> bindFn);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_init(void* ptr, delegate* unmanaged[Cdecl]<void*, void> initFn);

        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_set_local_init(void* ptr, delegate* unmanaged[Cdecl]<void*, void> localInitFn);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_supports_projection_pushdown(void* ptr, byte supportsProjectionPushdown);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_add_parameter(void* ptr, void* logicalType);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_table_function_add_named_parameter(void* ptr, string name, void* logicalType);
        #endregion


        #region Bind
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_bind_get_extra_info(void* bindInfo);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_bind_set_error(void* bindInfo, string error);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_bind_set_bind_data(void* bindInfo, void* data, delegate* unmanaged[Cdecl]<void*, void> deleteFn);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void  duckdb_bind_add_result_column(void* bindInfo, string name, void* logicalType); 
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_bind_set_cardinality(void* bindInfo, ulong cardinality, byte isExact);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial ulong duckdb_bind_get_parameter_count(void* bindInfo);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_bind_get_parameter(void* bindInfo, ulong index);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_bind_get_named_parameter(void* bindInfo, string name);
        
        
        #endregion

        #region Init

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_init_get_extra_info(void* initInfo);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_init_get_bind_data(void* initInfo);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_init_set_init_data(void* initInfo, void* initData, delegate* unmanaged[Cdecl]<void*, void> destroy);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial ulong duckdb_init_get_column_count(void* initInfo);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial ulong duckdb_init_get_column_index(void* initInfo, ulong columnIndex);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_init_set_max_threads(void* initInfo, ulong maxThreads);

        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_init_set_error(void* initInfo, string error);

        
        

        #endregion
        
        
        #region Function Scan
        // Function
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_function_get_extra_info(void* functionInfo);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_function_get_bind_data(void* functionInfo);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_function_get_init_data(void* functionInfo);

        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_function_set_error(void* functionInfo, string error);
        #endregion
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Fn(void* functionInfo, void* chunk)
    {
        try
        {
            var handle = GCHandle.FromIntPtr((IntPtr)Native.duckdb_function_get_extra_info(functionInfo));
            if (handle.Target is not ATableFunction func)
                throw new InvalidOperationException("Invalid function pointer");
            
            var bindData = GCHandle.FromIntPtr((IntPtr)Native.duckdb_function_get_bind_data(functionInfo));
            if (bindData.Target is not BindData bindDataObj)
                throw new InvalidOperationException("Invalid bind data pointer");
            
            var initData = GCHandle.FromIntPtr((IntPtr)Native.duckdb_function_get_init_data(functionInfo));
            if (initData.Target is not InitData initDataObj)
                throw new InvalidOperationException("Invalid init data pointer");

            var info = new FunctionInfo(functionInfo, chunk, bindDataObj, initDataObj);
            func.Execute(info);
        }
        catch (Exception e)
        {
            Native.duckdb_function_set_error(functionInfo, e.Message);
        }
    }

    protected abstract void Execute(FunctionInfo functionInfo);

    protected ref struct FunctionInfo
    {
        private void* _ptr;
        private readonly void* _chunk;
        private readonly BindData _bindData;
        private readonly InitData _initData;

        internal FunctionInfo(void* ptr, void* chunk, BindData bindData, InitData initData)
        {
            _ptr = ptr;
            _chunk = chunk;
            _bindData = bindData;
            _initData = initData;
        }
        
        public WritableChunk Chunk => new(_chunk);
        public int EmitSize => (int)GlobalConstants.DefaultVectorSize;

        public T GetBindInfo<T>()
        {
            if (_bindData.UserBindData is not T bindInfo)
                throw new InvalidOperationException("Cannot cast user bind data");
            return bindInfo;
        }

        /// <summary>
        /// Sets the emitted number of rows. Must be set at least once by every call to Execute
        /// </summary>
        public void SetEmittedRowCount(int count)
        {
            var chunk = Chunk;
            chunk.Size = (ulong)count;
        }

        public T GetInitInfo<T>()
        {
            if (_initData.UserData is not T initInfo)
                throw new InvalidOperationException("Cannot cast user init data");
            return initInfo;
        }

        public byte[] EngineToFn => _initData.EngineToFn;
        public sbyte[] FnToEngine => _initData.FnToEngine;
        
        /// <summary>
        /// Returns if the given column should be emitted, will be false if the engine has not requested
        /// this column's data
        /// </summary>
        public bool ShouldEmit(int column)
        {
            return _initData.FnToEngine[column] >= 0;
        }

        /// <summary>
        /// Gets the writable vector for the given column, if this column wasn't selected by the engine,
        /// the resulting column will have null internal pointers, and all attached methods will be undefined.
        /// </summary>
        public WritableVector GetWritableVector(int column)
        {
            if (column >= _initData.FnToEngine.Length)
                return default;
            var mappedIdx = _initData.FnToEngine[column];
            return mappedIdx >= 0 ? Chunk[(ulong)mappedIdx] : default;
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
            Native.duckdb_bind_set_error(bindInfo, e.ToString());
        }
    }

    protected abstract void Bind(BindInfo info);

    internal class BindData
    {
        /// <summary>
        /// User provided context data
        /// </summary>
        public object? UserBindData;
        
        /// <summary>
        /// The number of columns the function offered to the engine
        /// </summary>
        public int OfferedColumns;
    }
    
    public ref struct BindInfo
    {
        private void* _ptr;
        private readonly BindData _bindData;

        public BindInfo(void* ptr)
        {
            _ptr = ptr;
            _bindData = new BindData();
            var handle = GCHandle.Alloc(_bindData);
            Native.duckdb_bind_set_bind_data(_ptr, (void*)GCHandle.ToIntPtr(handle), &DeleteHandleFn);
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
            _bindData.UserBindData = bindInfo;
        }
        
        public void AddColumn<T>(string name)
        {
            using var logicalType = LogicalType.From<T>();
            Native.duckdb_bind_add_result_column(_ptr, name, logicalType._ptr);
            _bindData.OfferedColumns++;
        }

        public void AddColumn(string name, LogicalType logicalType)
        {
            Native.duckdb_bind_add_result_column(_ptr, name, logicalType._ptr);
            _bindData.OfferedColumns++;
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

    public class InitData
    {
        /// <summary>
        /// A mapping of the Engine's requested Column to the Fn's column
        /// </summary>
        public byte[] EngineToFn = [];
        
        /// <summary>
        /// A mapping of the Fn's column to the Engine's requested mapping, -1 means the column was not
        /// requested.
        /// </summary>
        public sbyte[] FnToEngine = [];

        /// <summary>
        /// User defined init data
        /// </summary>
        public object? UserData;
    }

    protected ref struct InitInfo
    {
        private void* _ptr;

        public InitInfo(void* ptr)
        {
            _ptr = ptr;
        }
        
        /// <summary>
        /// Sets the maximum number of threads DuckDB can use to call this function
        /// </summary>
        public void SetMaxThreads(int max)
        {
            Native.duckdb_init_set_max_threads(_ptr, (ulong)max);
        }

        public T GetBindData<T>()
        {
            var handle = GCHandle.FromIntPtr((IntPtr)Native.duckdb_init_get_bind_data(_ptr));
            if (handle.Target is not BindData bindData)
                throw new InvalidOperationException("Invalid bind data pointer");
            return bindData.UserBindData is T bindInfo ? bindInfo : throw new InvalidOperationException("Cannot cast user bind data");
        }
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void InitFn(void* initInfo)
    {
        try
        {
            var columns = Native.duckdb_init_get_column_count(initInfo);
            
            var fnHandle = GCHandle.FromIntPtr((IntPtr)Native.duckdb_init_get_extra_info(initInfo));
            if (fnHandle.Target is not ATableFunction fn)
                throw new InvalidOperationException("Invalid function pointer");
            
            var handle = GCHandle.FromIntPtr((IntPtr)Native.duckdb_init_get_bind_data(initInfo));
            if (handle.Target is not BindData bindData)
                throw new InvalidOperationException("Invalid bind data pointer");

            var initData = new InitData
            {
                EngineToFn = new byte[columns],
                FnToEngine = new sbyte[bindData.OfferedColumns]
            };
            
            // Set all columns to unused first
            initData.FnToEngine.AsSpan().Fill(-1);
            
            for (var i = 0UL; i < columns; i++)
            {
                var mapping = Native.duckdb_init_get_column_index(initInfo, i);
                initData.EngineToFn[i] = (byte)mapping;
                initData.FnToEngine[mapping] = (sbyte)i;
            }
            
            var userData = fn.Init(new InitInfo(initInfo), initData);
            initData.UserData = userData;
            
            Native.duckdb_init_set_init_data(initInfo, (void*)GCHandle.ToIntPtr(GCHandle.Alloc(initData)), &DeleteHandleFn);
        }
        catch (Exception ex)
        {
            Native.duckdb_init_set_error(initInfo, ex.ToString());
        }
    }

    protected virtual object? Init(InitInfo initInfo, InitData initData)
    {
        return null;
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void LocalInitFn(void* localInitInfo)
    {
    }
}
