using System;

namespace NexusMods.MnemonicDB.QueryEngine.Tables;

public class EmptyTable : ITable
{
    public static readonly EmptyTable Instance = new();
    
    public LVar[] Columns => [];

    public IColumn this[LVar column] => throw new IndexOutOfRangeException("Empty table has no columns");

    public IColumn this[int idx] => throw new IndexOutOfRangeException("Empty table has no columns");

    public IRowEnumerator EnumerateRows()
    {
        throw new Exception("Empty table has no rows");
    }
}
