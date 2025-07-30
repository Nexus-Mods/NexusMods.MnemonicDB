using System;
using System.Collections.Generic;
using ObservableCollections;

namespace NexusMods.HyperDuck.Adaptor.Impls.ResultAdaptors;

public class ObservableListAdaptor<TResult, TRowType, TRowAdaptor> : IResultAdaptor<TResult>
    where TResult : ObservableList<TRowType>
    where TRowAdaptor : IRowAdaptor<TRowType>
{
    
    public void Adapt(Result result, ref TResult value)
    {
        var columnCount = result.ColumnCount;
        Span<ReadOnlyVector> vectors = stackalloc ReadOnlyVector[(int)columnCount];
        
        int totalRows = 0;
        while (true)
        {
            using var chunk = result.FetchChunk();
            if (!chunk.IsValid)
                break;

            for (var v = 0; v < vectors.Length; v++)
                vectors[v] = chunk[(ulong)v];

            var cursor = new RowCursor(vectors);
            var listSize = value.Count;
            for (cursor.RowIndex = 0; cursor.RowIndex < (int)chunk.Size; cursor.RowIndex++)
            {
                if (totalRows < listSize)
                {
                    var current = value[totalRows];
                    TRowAdaptor.Adapt(cursor, ref current);
                    value[totalRows] = current!;
                }
                else
                {
                    TRowType? current = default;
                    TRowAdaptor.Adapt(cursor, ref current);
                    value.Add(current!);
                }
                totalRows++;
            }
        }

        var toRemove = value.Count - totalRows;
        if (toRemove > 0)
        {
            for (var i = 0; i < toRemove; i++)
            {
                value.RemoveAt(value.Count - 1);
            }
        }
    }
}

public class ObservableListAdaptorFactory : IResultAdaptorFactory
{
    public bool TryExtractRowType(ReadOnlySpan<Result.ColumnInfo> columns, Type resultType, out Type rowType, out int priority)
    {
        if (resultType.TryExtractGenericInterfaceArguments(typeof(ObservableList<>), out var genericArguments))
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
        return typeof(ObservableListAdaptor<,,>).MakeGenericType(resultType, rowType, rowAdaptorType);
    }
}
