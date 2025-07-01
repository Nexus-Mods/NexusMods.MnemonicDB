using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using DuckDB.NET.Native;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.QueryV2;

public class QueryResult<TRow, TLowLevelRow, TChunk> : IQueryResult<TRow>
    where TChunk : IDataChunk<TLowLevelRow, TChunk>
{
    private TChunk? _result;
    private DuckDBResult _dbResult;
    private int _chunkOffset = 0;
    private bool _isStarted = false;
    private readonly Func<TLowLevelRow, TRow> _converter;

    public QueryResult(DuckDBResult result, Func<TLowLevelRow, TRow> converter)
    {
        _converter = converter;
        _dbResult = result;
    }
    
    public bool MoveNext(out TRow result)
    {
        if (!_isStarted)
        {
            if (!TChunk.TryCreate(ref _dbResult, out _result))
            {
                _dbResult.Dispose();
            }

            _chunkOffset = 0;
            _isStarted = true;
        }

        if (_result!.TryGetRow(_chunkOffset, out var lowLevelRow))
        {
            result = _converter(lowLevelRow);
            _chunkOffset += 1;
            return true;
        }
           

        result = default!;
        return false;

    }

    public void Dispose()
    {
        _dbResult.Dispose();
    }
}

public static class QueryResultExtensions
{
    public static IQueryResult<TRow> ToQueryResult<TRow>(this DuckDBResult result)
    {
        var columns = NativeMethods.Query.DuckDBColumnCount(ref result);
        
        Span<DuckDBType> columnTypes = stackalloc DuckDBType[(int)columns];

        for (int i = 0; i < columnTypes.Length; i++)
        {
            columnTypes[i] = NativeMethods.Query.DuckDBColumnType(ref result, i);
        }
        
        var ctor = MakeQueryResultCtor(typeof(TRow), columnTypes);
        object converterFn = null!;
        if (columnTypes.Length == 1)
        {
            var param = Expression.Parameter(columnTypes[0].ToClrType(), "input");
            var convert = Expression.Convert(param, typeof(TRow));
            converterFn = Expression.Lambda(convert, param).Compile();
        }
        else
        {
            throw new NotImplementedException();
        }
        return (IQueryResult<TRow>)Activator.CreateInstance(ctor, result, converterFn)!;
    }

    private static Type MakeQueryResultCtor(Type rowType, ReadOnlySpan<DuckDBType> columnTypes)
    {
        if (rowType.IsAssignableTo(typeof(ITuple)) && columnTypes.Length != 1)
            return MakeQueryResultCtorForTuple(rowType, columnTypes);

        var vectorType = MakeVectorType(rowType, columnTypes[0]);
        var chunkType = MakeChunkType([vectorType]);
        
        var queryResultType = typeof(QueryResult<,,>).MakeGenericType(rowType, columnTypes[0].ToClrType(), chunkType);
        return queryResultType;
    }

    private static Type MakeQueryResultCtorForTuple(Type resultType, ReadOnlySpan<DuckDBType> columnTypes)
    {
        var elementTypes = resultType.GetGenericArguments();

        var internalTypes = new Type[columnTypes.Length];
        for (var i = 0; i < columnTypes.Length; i++)
            internalTypes[i] = columnTypes[i].ToClrType();

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



    private static (Type VectorType, Type LowLevelType) MakeVectorType(Type highLevelType, DuckDBType columnType)
    {
        var lowLevelType = columnType.ToClrType();
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
}
