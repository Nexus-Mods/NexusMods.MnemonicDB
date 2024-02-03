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

    public void AllDatomsWithTx<TSink>(TSink sink) where TSink : IDatomSinkWithTx
    {
        DuckDBResult result;
        if (DuckDBQuery(_connection, "SELECT E, A, V, T FROM Datoms", out result) != DuckDBState.Success)
        {
            _logger.LogError("Failed to query Datoms: {Error}", Marshal.PtrToStringAnsi(DuckDBResultError(ref result)));
            throw new InvalidOperationException("Failed to query Datoms: " + Marshal.PtrToStringAnsi(DuckDBResultError(ref result)));
        }

        var count = DuckDBRowCount(ref result);


        unsafe
        {
            Span<IntPtr> children = stackalloc IntPtr[NumUnionElements];
            Span<IntPtr> data = stackalloc IntPtr[NumUnionElements];
            Span<IntPtr> validity = stackalloc IntPtr[NumUnionElements];

            var chunkCount = DuckDBResultChunkCount(result);
            for (var idx = 0; idx < chunkCount; idx++)
            {
                var chunk = DuckDBResultGetChunk(result, idx);
                var chunkSize = DuckDBDataChunkGetSize(chunk);


                var eDataVector = DuckDBDataChunkGetVector(chunk, 0);
                var eData = (ulong*)DuckDBVectorGetData(eDataVector);

                var aDataVector = DuckDBDataChunkGetVector(chunk, 1);
                var aData = (ulong*)DuckDBVectorGetData(aDataVector);

                var vData = DuckDBDataChunkGetVector(chunk, 2);
                var vType = DuckDBVectorGetColumnType(vData);

                var logicalTypeCount = DuckDBStructTypeChildCount(vType);


                var txDataVector = DuckDBDataChunkGetVector(chunk, 3);
                var txData = (ulong*)DuckDBVectorGetData(txDataVector);

                for (var childId = 1; childId < NumUnionElements; childId++)
                {
                    children[childId] = DuckDBStructVectorGetChild(vData, childId);
                    data[childId] = (IntPtr)DuckDBVectorGetData(children[childId]);
                    validity[childId] = (IntPtr)DuckDBVectorGetValidity(children[childId]);
                }

                for (var row = 0; row < chunkSize; row++)
                {

                    var e = eData[row];
                    var a = aData[row];

                    var validChild = ValidChild(validity, row);
                    var tx = txData[row];

                    switch (validChild)
                    {
                        case 1:
                            sink.Datom(e, a, ((long*)data[1])[row], tx);
                            break;
                        case 2:
                            sink.Datom(e, a, ((ulong*)data[2])[row], tx);
                            break;
                        case 3:
                            sink.Datom(e, a, ReadString((void*)data[3], row), tx);
                            break;
                        case 4:
                            throw new NotImplementedException();
                        case 5:
                            sink.Datom(e, a, ((double*)data[5])[row], tx);
                            break;
                        case 6:
                            sink.Datom(e, a, ((float*)data[6])[row], tx);
                            break;
                        case 7:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        DuckDBDestroyResult(ref result);

    }

    private unsafe int ValidChild(Span<IntPtr> validity, int row)
    {
        var idx = row >> 16;
        var vector = Vector128.Create(0,
            ((ushort*)validity[1])[idx],
            ((ushort*)validity[2])[idx],
            ((ushort*)validity[3])[idx],
            ((ushort*)validity[4])[idx],
            ((ushort*)validity[5])[idx],
            ((ushort*)validity[6])[idx],
            0);

        var shifted = Sse2.ShiftRightLogical(vector, (byte)(row % 16));
        shifted &= Vector128<ushort>.One;

        var mask = Sse2.CompareEqual(shifted, Vector128<ushort>.One).AsByte();
        var maskInt = Sse2.MoveMask(mask);

        int index = BitOperations.TrailingZeroCount(maskInt) >> 1;

        return index;
    }

    private unsafe string ReadString(void* ptr, int row)
    {
        var data = (DuckDBString2*)ptr + row;

        return new string(data->Data, 0, data->Length, Encoding.UTF8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe bool IsRowValid(ulong* validity, int row)
    {
        var mask = 1UL << (row & 63);
        return (validity[row >> 6] & mask) != 0;
    }

}

public partial struct DuckDBString2
{
    public _value_e__Union value;

    private const int InlineStringMaxLength = 12;

    public readonly int Length => (int)value.inlined.length;

    public readonly unsafe sbyte* Data
    {
        get
        {
            if (Length <= InlineStringMaxLength)
            {
                fixed (sbyte* pointerToFirst = value.inlined.inlined)
                {
                    return pointerToFirst;
                }
            }
            else
            {
                return value.pointer.ptr;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _value_e__Union
    {
        [FieldOffset(0)]
        public DuckDBStringPointer pointer;

        [FieldOffset(0)]
        public DuckDBStringInlined inlined;

        public unsafe partial struct DuckDBStringPointer
        {
            public uint length;

            public fixed sbyte prefix[4];

            public sbyte* ptr;
        }

        public unsafe partial struct DuckDBStringInlined
        {
            public uint length;

            public fixed sbyte inlined[12];
        }
    }
}
