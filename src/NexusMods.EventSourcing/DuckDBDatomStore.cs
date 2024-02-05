using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using DuckDB.NET;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.Paths;
using static DuckDB.NET.NativeMethods.Appender;
using static DuckDB.NET.NativeMethods.DataChunks;
using static DuckDB.NET.NativeMethods.LogicalType;
using static DuckDB.NET.NativeMethods.Query;
using static DuckDB.NET.NativeMethods.Startup;
using static DuckDB.NET.NativeMethods.Types;

namespace NexusMods.EventSourcing;

public class DuckDBDatomStore : IDatomStore
{
    public DuckDBDatomStore(ILogger<DuckDBDatabase> logger)
    {
        _logger = logger;
        _logger.LogInformation("Creating DuckDBDatomStore with in-memory database");
        DuckDBOpen(":memory:", out _db);
        DuckDBConnect(_db, out _connection);

        SetupDatabase();
    }

    private void SetupDatabase()
    {
        var result = new DuckDBResult();
        try
        {
            if (DuckDBQuery(_connection, CreateDatomsTable, out result) != DuckDBState.Success)
            {
                var msg = Marshal.PtrToStringAnsi(DuckDBResultError(ref result));
                _logger.LogError("Failed to create Datoms table: {Error}", msg);
                throw new InvalidOperationException("Failed to create Datoms table");
            }
        }
        finally
        {
            DuckDBDestroyResult(ref result);
        }
    }


    private static string UnionType = @"
       UNION (
            I BIGINT,
            U UBIGINT,
            S VARCHAR,
            B BOOLEAN,
            D DOUBLE,
            F FLOAT,
            Z BLOB
        )";

    private const int NumUnionElements = 8;

    private static string CreateDatomsTable = @$"
        CREATE TABLE IF NOT EXISTS Datoms (
            E UBIGINT NOT NULL,
            A UBIGINT NOT NULL,
            V {UnionType} NOT NULL,
            T UBIGINT NOT NULL
        )";

    private readonly AbsolutePath _path;
    private readonly DuckDBDatabase _db;
    private readonly DuckDBNativeConnection _connection;
    private readonly ILogger<DuckDBDatabase> _logger;
    public void Transact(params (ulong E, ulong A, object V, ulong Tx)[] source)
    {
        var appender = new DuckDBAppender();
        if (DuckDBAppenderCreate(_connection, null, "Datoms", out appender) != DuckDBState.Success)
        {
            _logger.LogError("Failed to create Appender");
            throw new InvalidOperationException("Failed to create Appender");
        }

        for (var idx = 0; idx < source.Length; idx++)
        {
            var (e, a, v, tx) = source[idx];
            if (DuckDBAppendUInt64(appender, e) != DuckDBState.Success)
            {
                _logger.LogError("Failed to append E");
                throw new InvalidOperationException("Failed to append E");
            }
            if (DuckDBAppendUInt64(appender, a) != DuckDBState.Success)
            {
                _logger.LogError("Failed to append A");
                throw new InvalidOperationException("Failed to append A");
            }

            AppendValue(appender, v);

            if (DuckDBAppendUInt64(appender, tx) != DuckDBState.Success)
            {
                _logger.LogError("Failed to append Tx");
                throw new InvalidOperationException("Failed to append Tx");
            }

            if (DuckDBAppenderEndRow(appender) != DuckDBState.Success)
            {
                _logger.LogError("Failed to end row");
                throw new InvalidOperationException("Failed to end row");
            }
        }

        if (DuckDBAppenderClose(appender) != DuckDBState.Success)
        {
            _logger.LogError("Failed to close appender");
            throw new InvalidOperationException("Failed to close appender");
        }

        _logger.LogDebug("Appended {Count} datoms", source.Length);

    }

    private void AppendValue(DuckDBAppender appender, object o)
    {
        DuckDBState state;
        switch (o)
        {
            case int i:
                state = DuckDBAppendInt32(appender, i);
                break;
            case float f:
                state = DuckDBAppendFloat(appender, f);
                break;
            case string s:
            {
                // TODO: Optimize this
                var bytes = Encoding.UTF8.GetBytes(s);
                var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                Marshal.WriteByte(ptr, bytes.Length, 0);
                using var handle = new SafeUnmanagedMemoryHandle(ptr, true);
                state = DuckDBAppendVarchar(appender, handle);
            }
                break;
            case bool b:
                state = DuckDBAppendBool(appender, b);
                break;
            case long l:
                state = DuckDBAppendInt64(appender, l);
                break;
            case ulong u:
                state = DuckDBAppendUInt64(appender, u);
                break;
            case double d:
                state = DuckDBAppendDouble(appender, d);
                break;
            case byte[] b:
                unsafe
                {
                    fixed (void* ptr = b)
                    {
                        state = ExtraNativeMethods.DuckDBAppendBlob(appender, ptr, b.Length);
                        break;
                    }
                }
            default:
                throw new Exception("Unsupported type: " + o.GetType());
        }
        if (state != DuckDBState.Success)
        {
            var error = Marshal.PtrToStringAnsi(DuckDBAppenderError(appender));
            _logger.LogError("Failed to append value: {Message}", error);
            throw new InvalidOperationException("Failed to append value" + error);
        }
    }

    public void AllDatomsWithTx<TSink>(in TSink sink) where TSink : IResultSetSink
    {
        var result = new DuckDBResultSet();
        if (DuckDBQuery(_connection, "SELECT E, A, V, T FROM Datoms", out result.Result) != DuckDBState.Success)
        {
            _logger.LogError("Failed to query Datoms: {Error}", Marshal.PtrToStringAnsi(DuckDBResultError(ref result.Result)));
            throw new InvalidOperationException("Failed to query Datoms: " + Marshal.PtrToStringAnsi(DuckDBResultError(ref result.Result)));
        }
        result.Init();
        sink.Process(ref result);

        DuckDBDestroyResult(ref result.Result);

    }
}
