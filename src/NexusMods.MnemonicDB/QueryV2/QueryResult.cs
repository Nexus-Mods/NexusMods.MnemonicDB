using System;
using System.Collections.Generic;
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

