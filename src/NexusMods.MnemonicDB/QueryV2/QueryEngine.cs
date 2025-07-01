using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DuckDB.NET.Native;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.QueryV2;

public class QueryEngine : IDisposable
{
    private readonly DuckDBDatabase _duckDb;
    private DuckDBNativeConnection _dbConnection;
    
    private const string ConnectionString = ":memrory:";
    
    private IConnection? _defaultConnection;
    
    private Dictionary<ValueTag, DuckDBLogicalType> _logicalTypes = new();
    
    public QueryEngine(IEnumerable<ModelTableDefinition> modelTables)
    {
        if (NativeMethods.Startup.DuckDBOpen(":memory:", out _duckDb) == DuckDBState.Error)
        {
            throw new InvalidOperationException("Failed to open DuckDB in memory.");
        }
        
        if (NativeMethods.Startup.DuckDBConnect(_duckDb, out _dbConnection) == DuckDBState.Error)
        {
            throw new InvalidOperationException("Failed to connect to DuckDB.");
        }

        foreach (var function in modelTables)
        {
            new ModelTableFunction(function, this).Register(_dbConnection);
        }
        
        SetupLogicalTypes();
    }

    public DuckDBLogicalType GetLogicalType(ValueTag tag)
    {
        if (_logicalTypes.TryGetValue(tag, out var logicalType))
        {
            return logicalType;
        }

        throw new InvalidOperationException($"Logical type for {tag} is not registered.");
    }
    
    private void SetupLogicalTypes()
    {
        _logicalTypes[ValueTag.UInt8] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.UnsignedTinyInt);
        _logicalTypes[ValueTag.UInt16] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.UnsignedSmallInt);
        _logicalTypes[ValueTag.UInt32] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.UnsignedInteger);
        _logicalTypes[ValueTag.UInt64] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.UnsignedBigInt);
        _logicalTypes[ValueTag.UInt128] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.UnsignedHugeInt);
        _logicalTypes[ValueTag.Int16] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.SmallInt);
        _logicalTypes[ValueTag.Int32] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.Integer);
        _logicalTypes[ValueTag.Int64] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.BigInt);
        _logicalTypes[ValueTag.Int128] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.HugeInt);
        _logicalTypes[ValueTag.Float32] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.Float);
        _logicalTypes[ValueTag.Float64] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.Double);
        _logicalTypes[ValueTag.Utf8] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.Varchar);
        _logicalTypes[ValueTag.Utf8Insensitive] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.Varchar);
        _logicalTypes[ValueTag.Blob] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.Blob);
        _logicalTypes[ValueTag.HashedBlob] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.Blob);
        _logicalTypes[ValueTag.Reference] = HighPerfBindings.DuckDBCreateLogicalType(DuckDBType.UnsignedBigInt);
        _logicalTypes[ValueTag.Tuple2_UShort_Utf8I] = HighPerfBindings.DuckDBCreateStructType(
            [_logicalTypes[ValueTag.UInt16], _logicalTypes[ValueTag.Utf8Insensitive]],
            ["LocationId", "Path"], 2);
        _logicalTypes[ValueTag.Tuple3_Ref_UShort_Utf8I] = HighPerfBindings.DuckDBCreateStructType(
            [_logicalTypes[ValueTag.Reference], _logicalTypes[ValueTag.UInt16], _logicalTypes[ValueTag.Utf8Insensitive]],
            ["Parent", "LocationId", "Path"], 3);
    }

    public void Add(IConnection connection)
    {
        _defaultConnection = connection;
    }

    public IConnection DefaultConnection()
    {
        return _defaultConnection!;
    }
    
    [MustDisposeResource]
    public IQueryResult<TRow> Query<TRow>(string sql)
    {
        if (NativeMethods.Query.DuckDBQuery(_dbConnection, sql, out var res) == DuckDBState.Error)
        {
            var errorString = Marshal.PtrToStringUTF8(NativeMethods.Query.DuckDBResultError(ref res));
            throw new InvalidOperationException("Failed to execute query: " + errorString);
        }

        return ToQueryResult<TRow>(res);
    }
    
    private IQueryResult<TRow> ToQueryResult<TRow>(DuckDBResult result)
    {
        var columns = NativeMethods.Query.DuckDBColumnCount(ref result);
        
        Span<DuckDBLogicalType> columnTypes = stackalloc DuckDBLogicalType[(int)columns];

        for (int i = 0; i < columnTypes.Length; i++)
        {
            columnTypes[i] = HighPerfBindings.DuckDBColumnLogicalType(ref result, i);
        }
        
        var ctor = MakeQueryResultCtor(typeof(TRow), columnTypes);
        object converterFn = null!;
        if (columnTypes.Length == 1)
        {
            var param = Expression.Parameter(ToClrType(columnTypes[0]), "input");
            var convert = Expression.Convert(param, typeof(TRow));
            converterFn = Expression.Lambda(convert, param).Compile();
        }
        else
        {
            var tupleType = typeof(TRow).GetGenericTypeDefinition();
            var outputElements = typeof(TRow).GetGenericArguments();

            var param = Expression.Parameter(tupleType.MakeGenericType(columnTypes.ToArray().Select(ToClrType).ToArray()), "input");
            var decompose = new List<Expression>();
            for (var i = 0; i < columnTypes.Length; i++)
            {
                decompose.Add(Expression.Convert(Expression.PropertyOrField(param, "Item" + (i + 1)), outputElements[i]));
            }

            var ctorExpr = Expression.New(typeof(TRow).GetConstructor(outputElements)!, decompose);
            converterFn = Expression.Lambda(ctorExpr, param).Compile();
        }
        
        foreach (var columnType in columnTypes) 
            columnType.Dispose();
        
        return (IQueryResult<TRow>)Activator.CreateInstance(ctor, result, converterFn)!;
    }
    
    private Type MakeQueryResultCtor(Type rowType, ReadOnlySpan<DuckDBLogicalType> columnTypes)
    {
        if (rowType.IsAssignableTo(typeof(ITuple)) && columnTypes.Length != 1)
            return MakeQueryResultCtorForTuple(rowType, columnTypes);

        var vectorType = MakeVectorType(rowType, columnTypes[0]);
        var chunkType = MakeChunkType([vectorType]);
        
        var queryResultType = typeof(QueryResult<,,>).MakeGenericType(rowType, ToClrType(columnTypes[0]), chunkType);
        return queryResultType;
    }

    private Type MakeQueryResultCtorForTuple(Type resultType, ReadOnlySpan<DuckDBLogicalType> columnTypes)
    {
        var elementTypes = resultType.GetGenericArguments();

        var internalTypes = new Type[columnTypes.Length];
        for (var i = 0; i < columnTypes.Length; i++)
            internalTypes[i] = ToClrType(columnTypes[i]);

        var internalResultType = resultType.GetGenericTypeDefinition().MakeGenericType(internalTypes);
        
        var vectorTypes = elementTypes.Zip(columnTypes.ToArray(), MakeVectorType).ToArray();
        var chunkType = MakeChunkType(vectorTypes);
        
        var queryResultType = typeof(QueryResult<,,>).MakeGenericType(resultType, internalResultType, chunkType);
        return queryResultType;
    }

    private static Type MakeChunkType((Type VectorType, Type LowLevelType)[] vectorTypes)
    {
        var types = new Type[vectorTypes.Length * 2];
        
        for (int idx = 0; idx < vectorTypes.Length; idx++)
        {
            types[idx] = vectorTypes[idx].VectorType;
            types[idx + vectorTypes.Length] = vectorTypes[idx].LowLevelType;
        }
        var chunkType = DataChunkForArity(vectorTypes.Length).MakeGenericType(types);
        return chunkType;
    }

    private static Type DataChunkForArity(int arity)
    {
        return arity switch
        {
            1 => typeof(DataChunk<,>),
            2 => typeof(DataChunk<,,,>),
            _ => throw new NotImplementedException($"No override for {arity}")
        };
    }

    private static Type TupleTypeForArity(int arity)
    {
        return arity switch
        {
            2 => typeof(ValueTuple<,>),
            3 => typeof(ValueTuple<,,>),
            4 => typeof(ValueTuple<,,,>),
            _ => throw new NotSupportedException()
        };
    }



    private (Type VectorType, Type LowLevelType) MakeVectorType(Type highLevelType, DuckDBLogicalType columnType)
    {
        var lowLevelType = ToClrType(columnType);
        return (typeof(Vector<>).MakeGenericType(lowLevelType), lowLevelType);
    }

    private static ValueTag GetValueTag(DuckDBType duckDbColumnType)
    {
        return duckDbColumnType switch
        {
            DuckDBType.Integer => ValueTag.Int32,
            DuckDBType.UnsignedBigInt => ValueTag.UInt64,
            DuckDBType.Varchar => ValueTag.Utf8,
            _ => throw new ArgumentOutOfRangeException(nameof(duckDbColumnType), duckDbColumnType, null)
        };
    }
    private Type ToClrType(DuckDBLogicalType logicalType)
    {
        var typeId = HighPerfBindings.DuckDBGetTypeId(logicalType);
        return typeId switch
        {
            DuckDBType.Boolean => typeof(bool),
            DuckDBType.Varchar => typeof(string),
            DuckDBType.UnsignedTinyInt => typeof(byte),
            DuckDBType.UnsignedSmallInt => typeof(ushort),
            DuckDBType.UnsignedInteger => typeof(uint),
            DuckDBType.UnsignedBigInt => typeof(ulong),
            DuckDBType.UnsignedHugeInt => typeof(UInt128),
            DuckDBType.SmallInt => typeof(short),
            DuckDBType.Integer => typeof(int),
            DuckDBType.BigInt => typeof(long),
            DuckDBType.HugeInt => typeof(Int128),
            DuckDBType.Float => typeof(float),
            DuckDBType.Double => typeof(double),
            DuckDBType.Blob => typeof(byte[]),
            DuckDBType.Struct => StructToClrType(logicalType),
            _ => throw new NotImplementedException()
        };
    }

    private Type StructToClrType(DuckDBLogicalType logicalType)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _dbConnection.Dispose();
        _duckDb.Dispose();
    }

    public void Register(TableFunction tableFunction)
    {
        tableFunction.Register(_dbConnection);
    }
}
