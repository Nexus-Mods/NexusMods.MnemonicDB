using System;
using R3;

namespace NexusMods.HyperDuck.Adaptor.Impls.ResultAdaptors;

public class ObserverAdaptor<TResult, TValue, TRowAdaptor> : IResultAdaptor<TResult> 
    where TRowAdaptor : IRowAdaptor<TValue>
    where TResult : Observer<TValue>
{
    public void Adapt(Result result, ref TResult value)
    {
        TValue row = default!;
        var columnCount = result.ColumnCount;
        Span<ReadOnlyVector> vectors = stackalloc ReadOnlyVector[(int)columnCount];
        var cursor = new RowCursor(vectors);
        
        using var chunk = result.FetchChunk();
        if (chunk.Size == 0)
            return;

        if (chunk.Size > 0)
        {
            value.OnErrorResume(new InvalidOperationException("Too many rows returned."));
        }
        
        cursor.RowIndex = 0;
        TRowAdaptor.Adapt(cursor, ref row!);
        value.OnNext(row);
    }
}

public class ObserverAdaptorFactory : IResultAdaptorFactory
{
    public bool TryExtractRowType(ReadOnlySpan<Result.ColumnInfo> columns, Type resultType, out Type rowType, out int priority)
    {
        if (resultType.TryExtractGenericInterfaceArguments(typeof(Observer<>), out var genericArguments))
        {
            rowType = genericArguments[0];
            priority = 1;
            return true;
        }
        rowType = default!;
        priority = 0;
        return false;
    }

    public Type CreateType(Type resultType, Type rowType, Type rowAdaptorType)
    {
        return typeof(ObserverAdaptor<,,>).MakeGenericType(resultType, rowType, rowAdaptorType);
    }
}
