using System;
using System.Collections;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine.Tables;

public class ListColumn<T> : IColumn<T>, IAppendableColumn<T>
{
    private readonly List<T> _values;
    public Type Type => typeof(T);
    
    public ListColumn()
    {
        _values = [];
    }
    public IEnumerator<T> GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _values.Count;

    public T this[int row] => _values[row];

    public void Add(T value)
    {
        _values.Add(value);
    }

    public void AddFrom(TableJoiner.JoinerEnumerator e, int srcColumn)
    {
        _values.Add(e.Get<T>(srcColumn));
    }
}
