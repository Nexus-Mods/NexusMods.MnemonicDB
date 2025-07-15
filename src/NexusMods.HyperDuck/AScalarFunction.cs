using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.HyperDuck;

public abstract unsafe partial class AScalarFunction
{
    private void* _ptr;
    
    protected AScalarFunction()
    {
    }

    public void Register(Connection connection)
    {
        _ptr = Native.duckdb_create_scalar_function();
        Setup();
        Native.duckdb_scalar_function_set_function(_ptr, &Fn);
        var handle = GCHandle.Alloc(this);
        Native.duckdb_scalar_function_set_extra_info(_ptr, (void*)GCHandle.ToIntPtr(handle), &DeleteHandleFn);
        if (Native.duckdb_register_scalar_function(connection._ptr, _ptr) != State.Success)
            throw new InvalidOperationException("Failed to register scalar function.");
        Native.duckdb_destroy_scalar_function(ref _ptr);
    }

    public abstract void Setup();

    /// <summary>
    /// Sets the name of the function
    /// </summary>
    protected void SetName(string name)
    {
        Native.duckdb_scalar_function_set_name(_ptr, name);
    }

    /// <summary>
    /// Add a required, unnamed parameter
    /// </summary>
    public void AddParameter<T>()
    {
        using var logicalType = LogicalType.From<T>();
        Native.duckdb_scalar_function_add_parameter(_ptr, logicalType._ptr);
    }

    /// <summary>
    /// Set the return type of the parameter
    /// </summary>
    public void SetReturnType<T>()
    {
        using var logicalType = LogicalType.From<T>();
        Native.duckdb_scalar_function_set_return_type(_ptr, logicalType._ptr);
    }

    public abstract void Execute(ReadOnlyChunk chunk, WritableVector vector);

    internal static partial class Native
    {
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* duckdb_create_scalar_function();
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_scalar_function_set_name(void* ptr, string name);

        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_scalar_function_add_parameter(void* ptr, void* logicalType);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_scalar_function_set_return_type(void* ptr, void* logicalType);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_scalar_function_set_function(void* ptr, delegate* unmanaged[Cdecl]<void*, void*, void*, void> fnPtr);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_scalar_function_set_extra_info(void* ptr, void* extraInfo, delegate* unmanaged[Cdecl]<void*, void> deleteFn);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void *duckdb_scalar_function_get_extra_info(void* ptr);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial State duckdb_register_scalar_function(void* conn, void* fn);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_destroy_scalar_function(ref void* fn);
        
        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void  duckdb_scalar_function_set_error(void* fn, string error);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Fn(void* fn, void* inputChunk, void* outputVector)
    {
        try
        {
            var obj = GCHandle.FromIntPtr((IntPtr)Native.duckdb_scalar_function_get_extra_info(fn));
            if (obj.Target is not AScalarFunction func)
                throw new InvalidOperationException("Invalid function pointer");
            var chunk = new ReadOnlyChunk(inputChunk);
            func.Execute(chunk, new WritableVector(outputVector, chunk.Size));
        }
        catch (Exception e)
        {
            Native.duckdb_scalar_function_set_error(fn, e.ToString());
        }
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void DeleteHandleFn(void* fn)
    {
        var handle = GCHandle.FromIntPtr((IntPtr)fn);
        handle.Free();
    }
}
