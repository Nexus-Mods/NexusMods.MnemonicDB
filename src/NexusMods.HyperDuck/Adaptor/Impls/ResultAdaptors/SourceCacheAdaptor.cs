using System;
using System.Collections.Generic;
using DynamicData;

namespace NexusMods.HyperDuck.Adaptor.Impls.ResultAdaptors;

public class SourceCacheAdaptor<TRowType, TKey, TRowAdaptor> : IResultAdaptor<SourceCache<TRowType, TKey>>
    where TRowAdaptor : IRowAdaptor<TRowType>
    where TRowType : notnull
    where TKey : notnull
{
    
    public void Adapt(Result result, ref SourceCache<TRowType, TKey> value)
    {
        var columnCount = result.ColumnCount;
        
        value.Edit(updater => {
            updater.Clear();
            Span<ReadOnlyVector> vectors = stackalloc ReadOnlyVector[(int)columnCount];
            while (true)
            {
                using var chunk = result.FetchChunk();
                if (!chunk.IsValid)
                    break;

                for (var v = 0; v < vectors.Length; v++)
                    vectors[v] = chunk[(ulong)v];

                var cursor = new RowCursor(vectors);
                for (cursor.RowIndex = 0; cursor.RowIndex < (int)chunk.Size; cursor.RowIndex++)
                {
                    TRowType? current = default;
                    TRowAdaptor.Adapt(cursor, ref current);
                    updater.AddOrUpdate(current!);
                }
            }
        });
    }
}

public class SourceCacheAdaptorFactory : IResultAdaptorFactory
{
    public bool TryExtractRowType(ReadOnlySpan<Result.ColumnInfo> columns, Type resultType, out Type rowType, out int priority)
    {
        if (resultType.TryExtractGenericInterfaceArguments(typeof(SourceCache<,>), out var genericArguments))
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
        resultType.TryExtractGenericInterfaceArguments(typeof(SourceCache<,>), out var kvTypes);
        
        return typeof(SourceCacheAdaptor<,,>).MakeGenericType(rowType, kvTypes[1], rowAdaptorType);
    }
}
