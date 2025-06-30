using System;
using System.Linq;
using System.Runtime.CompilerServices;
using DuckDB.NET.Native;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.QueryV2;

public class QueryResult<TRow, TChunk> : IQueryResult<TRow>
    where TChunk : IDataChunk<TRow, TChunk>
{
    private TChunk? _result;
    private DuckDBResult _dbResult;
    private bool isStarted = false;
    private int _chunkOffset = 0;
    private bool _isStarted;

    public QueryResult(DuckDBResult result)
    {
        _dbResult = result;
    }
    
    public bool MoveNext(out TRow result)
    {
        if (!isStarted)
        {
            if (!TChunk.TryCreate(ref _dbResult, out _result))
            {
                _dbResult.Dispose();
            }

            _chunkOffset = 0;
            _isStarted = true;
        }

        if (_result!.TryGetRow(_chunkOffset, out result))
            return true;

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
        
        Span<ValueTag> columnTypes = stackalloc ValueTag[(int)columns];

        for (int i = 0; i < columnTypes.Length; i++)
        {
            columnTypes[i] = GetValueTag(NativeMethods.Query.DuckDBColumnType(ref result, i));
        }
        
        var ctor = MakeQueryResultCtor(typeof(TRow), columnTypes);
        return (IQueryResult<TRow>)Activator.CreateInstance(ctor, result)!;
    }

    private static Type MakeQueryResultCtor(Type rowType, Span<ValueTag> columnTypes)
    {
        if (rowType.IsAssignableTo(typeof(ITuple)) && columnTypes.Length != 1)
            return MakeQueryResultCtorForTuple(rowType, columnTypes);

        var vectorType = MakeVectorType(rowType, columnTypes[0]);
        var chunkType = MakeChunkType([vectorType]);
        
        var queryResultType = typeof(QueryResult<,>).MakeGenericType(rowType, chunkType);
        return queryResultType;
    }

    private static Type MakeQueryResultCtorForTuple(Type resultType, Span<ValueTag> columnTypes)
    {
        var elementTypes = resultType.GetGenericArguments();
        
        var vectorTypes = elementTypes.Zip(columnTypes.ToArray(), MakeVectorType).ToArray();
        var chunkType = MakeChunkType(vectorTypes);
        
        var queryResultType = typeof(QueryResult<,>).MakeGenericType(resultType, chunkType);
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



    private static (Type VectorType, Type LowLevelType) MakeVectorType(Type type, ValueTag columnType)
    {
        var lowLevelType = columnType.LowLevelType();

        if (lowLevelType.IsAssignableTo(type))
            return (typeof(Vector<,>).MakeGenericType(type, type), lowLevelType);

        throw new NotImplementedException();
    }

    private static ValueTag GetValueTag(DuckDBType duckDbColumnType)
    {
        return duckDbColumnType switch
        {
            DuckDBType.Integer => ValueTag.Int32,
            DuckDBType.Varchar => ValueTag.Utf8,
            _ => throw new ArgumentOutOfRangeException(nameof(duckDbColumnType), duckDbColumnType, null)
        };
    }
}
