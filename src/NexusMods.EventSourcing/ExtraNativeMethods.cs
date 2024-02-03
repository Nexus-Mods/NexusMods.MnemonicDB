using System.Runtime.InteropServices;
using DuckDB.NET;

namespace NexusMods.EventSourcing;

public static unsafe class ExtraNativeMethods
{
    private const string DuckDbLibrary = "duckdb";

    #if NET5_0_OR_GREATER
        [SuppressGCTransition]
    #endif
        [DllImport(DuckDbLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "duckdb_append_blob")]
        public static extern DuckDBState DuckDBAppendBlob(DuckDBAppender appender, void* data, int length);

}
