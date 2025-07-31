using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using NexusMods.HyperDuck.Adaptor;
using NexusMods.HyperDuck.Exceptions;

namespace NexusMods.HyperDuck;

public unsafe partial struct Database : IDisposable
{
    private void *_ptr;
    
    [MustDisposeResource]
    public static Database Open(string path)
    {
        var state = new Database();
        var result = Native.duckdb_open(path, ref state._ptr);
        if (result != State.Success)
        {
            throw new OpenDatabaseException();
        }
        return state;
    }


    /// <summary>
    /// Open an in-memory database.
    /// </summary>
    public static Database OpenInMemory()
    {
        return Open(":memory:");
    }

    public Connection Connect()
    {
        ArgumentNullException.ThrowIfNull(_ptr);
        var conPtr = default(void*);
        if (Native.duckdb_connect( _ptr, ref conPtr) != State.Success)
        {
            throw new ConnectException();
        }

        return new Connection(conPtr);
    }
    
    private static partial class Native
    {

        [LibraryImport(GlobalConstants.LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial State duckdb_open(string path, ref void* db);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void duckdb_close(ref void* db);
        
        [LibraryImport(GlobalConstants.LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial State duckdb_connect(void* db, ref void* connection);
    }

    public void Dispose()
    {
        if (_ptr == null) 
            return;
        
        Native.duckdb_close(ref _ptr);
        _ptr = null;
    }
}
