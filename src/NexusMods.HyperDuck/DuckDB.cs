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
    private IRegistry _registry;


    [MustDisposeResource]
    public static Database Open(string path, IRegistry registry)
    {
        var state = new Database();
        state._registry = registry;
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
    public static Database OpenInMemory(IRegistry registry)
    {
        return Open(":memory:", registry);
    }

    [MustDisposeResource]
    public Connection Connect()
    {
        ArgumentNullException.ThrowIfNull(_ptr);
        Connection connection = new Connection(_registry);
        if (Native.duckdb_connect( this._ptr, ref connection._ptr) != State.Success)
        {
            throw new ConnectException();
        }
        return connection;
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
        {
            return;
        }
        
        Native.duckdb_close(ref _ptr);
        _ptr = null;
    }
}
