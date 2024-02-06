using System;
using System.Runtime.InteropServices;
using DuckDB.NET;
using NexusMods.EventSourcing.Abstractions;
using static DuckDB.NET.NativeMethods.PreparedStatements;
using static DuckDB.NET.NativeMethods.Query;

namespace NexusMods.EventSourcing;

public class DuckDBPreparedStatement<T1> : IDisposable
{
    private DuckDBPreparedStatement _prepared;
    public DuckDBPreparedStatement(DuckDBNativeConnection conn, string sql)
    {
        if (DuckDBPrepare(conn, sql, out _prepared) != DuckDBState.Success)
        {
            var error = DuckDBPrepareError(_prepared);
            var msg = Marshal.PtrToStringAnsi(error);
            throw new Exception("Failed to prepare statement: "+ msg);
        }

        if (DuckDBParams(_prepared) != 1)
        {
            throw new Exception("Expected 1 parameter");
        }
    }

    public void BindAndExectute<TSink>(T1 v1, ref TSink sink) where TSink : IResultSetSink
    {
        switch (v1)
        {
            case ulong ul:
                DuckDBBindUInt64(_prepared, 1, ul);
                break;
            default:
                throw new Exception("TODO: Implement other types");
        }

        var resultSet = new DuckDBResultSet();
        if (DuckDBExecutePrepared(_prepared, out resultSet.Result) != DuckDBState.Success)
        {
            var error = DuckDBResultError(ref resultSet.Result);
            var msg = Marshal.PtrToStringAnsi(error);
            throw new Exception("Failed to execute prepared statement: "+ msg);
        }

        try
        {
            if (resultSet.Init())
                sink.Process(ref resultSet);
        }
        finally
        {
            DuckDBDestroyResult(ref resultSet.Result);
        }
    }

    public void Dispose()
    {
        _prepared.Dispose();
    }
}
