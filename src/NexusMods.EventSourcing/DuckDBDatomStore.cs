using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DuckDB.NET;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.BuiltinEntities;
using NexusMods.EventSourcing.Sinks;
using NexusMods.Paths;
using static DuckDB.NET.NativeMethods.Appender;
using static DuckDB.NET.NativeMethods.Query;
using static DuckDB.NET.NativeMethods.Startup;

namespace NexusMods.EventSourcing;

public class DuckDBDatomStore : IDatomStore
{
    public DuckDBDatomStore(ILogger<DuckDBDatomStore> logger)
    {
        _logger = logger;
        _logger.LogInformation("Creating DuckDBDatomStore with in-memory database");
        DuckDBOpen(":memory:", out _db);
        DuckDBConnect(_db, out _connection);

        SetupDatabase();
        var socket = new ArrayDatomSinkSocket(StaticData.InitialState());
        var nextid = 0UL;
        Transact(ref socket, ref nextid, new Dictionary<ulong, ulong>());
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
            H HUGEINT,
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
    private readonly ILogger<DuckDBDatomStore> _logger;


    private struct DatomSink(DuckDBAppender appender, ulong nextId, IDictionary<ulong, ulong> remaps) : IDatomSink
    {
        public ulong NextId => nextId;
        public void Emit(ulong e, ulong a, ulong v, ulong t)
        {
            DuckDBAppendUInt64(appender, MaybeRemap(e));
            DuckDBAppendUInt64(appender, a);
            DuckDBAppendUInt64(appender, v);
            DuckDBAppendUInt64(appender, t);
            DuckDBAppenderEndRow(appender);
        }

        public void Emit(ulong e, ulong a, long v, ulong t)
        {
            DuckDBAppendUInt64(appender, MaybeRemap(e));
            DuckDBAppendUInt64(appender, a);
            DuckDBAppendInt64(appender, v);
            DuckDBAppendUInt64(appender, t);
            DuckDBAppenderEndRow(appender);
        }

        public void Emit(ulong e, ulong a, double v, ulong t)
        {
            DuckDBAppendUInt64(appender, MaybeRemap(e));
            DuckDBAppendUInt64(appender, a);
            DuckDBAppendDouble(appender, v);
            DuckDBAppendUInt64(appender, t);
            DuckDBAppenderEndRow(appender);
        }

        public void Emit(ulong e, ulong a, float v, ulong t)
        {
            DuckDBAppendUInt64(appender, MaybeRemap(e));
            DuckDBAppendUInt64(appender, a);
            DuckDBAppendFloat(appender, v);
            DuckDBAppendUInt64(appender, t);
            DuckDBAppenderEndRow(appender);
        }

        public void Emit(ulong e, ulong a, string v, ulong t)
        {
            DuckDBAppendUInt64(appender, MaybeRemap(e));
            DuckDBAppendUInt64(appender, a);
            var bytes = Encoding.UTF8.GetBytes(v);
            var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr, bytes.Length, 0);
            using var handle = new SafeUnmanagedMemoryHandle(ptr, true);
            DuckDBAppendVarchar(appender, handle);
            DuckDBAppendUInt64(appender, t);
            DuckDBAppenderEndRow(appender);
        }

        public void Emit(ulong e, ulong a, UInt128 v, ulong t)
        {
            DuckDBAppendUInt64(appender, MaybeRemap(e));
            DuckDBAppendUInt64(appender, a);
            var ddb = Unsafe.As<UInt128, DuckDBUHugeInt>(ref v);
            ExtraNativeMethods.DuckDBAppendUHugeInt(appender, ddb);
            DuckDBAppendUInt64(appender, t);
            DuckDBAppenderEndRow(appender);
        }

        public void Emit(ulong e, ulong a, ReadOnlySpan<byte> v, ulong t)
        {
            DuckDBAppendUInt64(appender, MaybeRemap(e));
            DuckDBAppendUInt64(appender, a);
            unsafe
            {
                fixed (void* ptr = v)
                {
                    ExtraNativeMethods.DuckDBAppendBlob(appender, ptr, v.Length);
                }
            }
            DuckDBAppendUInt64(appender, t);
            DuckDBAppenderEndRow(appender);
        }

        public void Emit(ulong e, ulong a, EntityId v, ulong t)
        {
            DuckDBAppendUInt64(appender, MaybeRemap(e));
            DuckDBAppendUInt64(appender, a);
            DuckDBAppendUInt64(appender, MaybeRemap(v.Value));
            DuckDBAppendUInt64(appender, t);
            DuckDBAppenderEndRow(appender);
        }

        private ulong MaybeRemap(ulong val)
        {
            if (!Ids.IsIdOfSpace(val, IdSpace.Temp)) return val;

            if (remaps.TryGetValue(val, out var remapped))
                return remapped;
            remapped = ++nextId;
            remaps.Add(val, remapped);
            return remapped;
        }
    }

    public void Transact<TSocket, TDict>(ref TSocket socket, ref ulong nextId, TDict remappedIds)
        where TSocket : IDatomSinkSocket
        where TDict : IDictionary<ulong, ulong>
    {
        DuckDBAppender appender;
        if (DuckDBAppenderCreate(_connection, null, "Datoms", out appender) != DuckDBState.Success)
        {
            _logger.LogError("Failed to create Appender");
            throw new InvalidOperationException("Failed to create Appender");
        }

        var sink = new DatomSink(appender, nextId, remappedIds);
        socket.Process(ref sink);
        nextId = sink.NextId;


        if (DuckDBAppenderClose(appender) != DuckDBState.Success)
        {
            _logger.LogError("Failed to close appender");
            throw new InvalidOperationException("Failed to close appender");
        }
    }

    public void QueryByE<TSink>(ulong e, ref TSink sink, ulong t) where TSink : IResultSetSink
    {
        var stmt = new DuckDBPreparedStatement<ulong>(_connection,
            """
            SELECT E, A, arg_max(V, T) V, arg_max(T, T) T
            FROM Datoms
            WHERE E = ?
            GROUP BY E, A
            ORDER BY E, A
            """);
        stmt.BindAndExectute(e, ref sink);
    }

    public List<DbRegisteredAttribute> GetDbAttributes()
    {
        var stmt = new DuckDBPreparedStatement<ulong>(_connection,
            """
            SELECT E, A, arg_max(V, T) V, arg_max(T, T) T
            FROM Datoms
            WHERE E <= ?
            GROUP BY E, A
            ORDER BY E, A
            """);
        var attrLoader = new AttributeLoader();
        stmt.BindAndExectute(Ids.MaxId(IdSpace.Attr), ref attrLoader);

        return attrLoader.Attributes;
    }

    public void AllDatomsWithTx<TSink>(in TSink sink) where TSink : IResultSetSink
    {
        var result = new DuckDBResultSet();
        if (DuckDBQuery(_connection, "SELECT E, A, V, T FROM Datoms", out result.Result) != DuckDBState.Success)
        {
            _logger.LogError("Failed to query Datoms: {Error}", Marshal.PtrToStringAnsi(DuckDBResultError(ref result.Result)));
            throw new InvalidOperationException("Failed to query Datoms: " + Marshal.PtrToStringAnsi(DuckDBResultError(ref result.Result)));
        }
        try
        {
            if (result.Init())
                sink.Process(ref result);
        }
        finally
        {
            DuckDBDestroyResult(ref result.Result);
        }
    }
}
