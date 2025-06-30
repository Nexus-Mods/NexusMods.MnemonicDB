using System;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryV2;

public interface IQueryResult<TRow> : IDisposable
{
    /// <summary>
    /// Move to the next result, returning false if there are no more items
    /// </summary>
    public bool MoveNext(out TRow result);


    /// <summary>
    /// Completely consume the query results into a List
    /// </summary>
    public List<TRow> ToList()
    {
        List<TRow> list = [];
        while (MoveNext(out var row))
        {
            list.Add(row);
        }
        return list;
    }
}
