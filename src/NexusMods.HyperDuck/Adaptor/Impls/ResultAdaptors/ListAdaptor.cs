using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace NexusMods.HyperDuck.Adaptor.Impls.ResultAdaptors;

public class ListAdaptor<TResult, TRowType, TRowAdaptor> : IResultAdaptor<TResult>
    where TResult : IList<TRowType>
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
                    var current = value[0];
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

        if (totalRows < value.Count)
        {
            for (int i = value.Count; i > totalRows; i--)
            {
                value.RemoveAt(i);
            }
        }
    }
}

public class ListAdaptorFactory : IResultAdaptorFactory
{
    public bool TryExtractRowType(Type resultType, out Type rowType, out int priority)
    {
        if (resultType.TryExtractGenericInterfaceArguments(typeof(List<>), out var genericArguments))
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
        return typeof(ListAdaptor<,,>).MakeGenericType(resultType, rowType, rowAdaptorType);
    }
}
