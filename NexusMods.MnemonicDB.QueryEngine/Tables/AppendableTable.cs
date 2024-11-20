using System;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine.Tables;

public class AppendableTable : ITable
{
    private readonly List<IColumn> _columnData;
    private readonly LVar[] _columnNames;

    public AppendableTable(LVar[] columns)
    {
        _columnNames = columns;
        _columnData = new List<IColumn>(columns.Length);
        foreach (var column in columns)
        {
            _columnData.Add(column.MakeColumn());
        }
    }

    public LVar[] Columns => _columnNames;
    public int Count { get; }

    public IColumn this[LVar column]
    {
        get
        {
            var idxOf = Array.IndexOf(_columnNames, column);
            return _columnData[idxOf];
        }
    }

    public IColumn this[int idx] => _columnData[idx];
    
    public IRowEnumerator EnumerateRows()
    {
        return new RowEnumerator(this);
    }
    
    private class RowEnumerator : IRowEnumerator
    {
        private readonly AppendableTable _table;
        private int _idx;

        internal RowEnumerator(AppendableTable table)
        {
            _table = table;
            _idx = -1;
        }
        
        public bool MoveNext()
        {
            if (_idx < _table.Count)
            {
                _idx++;
                return true;
            }
            return false;
        }

        public LVar[] Columns => _table.Columns;
        
        public T Get<T>(LVar column)
        {
#if DEBUG
            if (column.Type != typeof(T))
            {
                throw new InvalidOperationException($"Column {column} is not of type {typeof(T)}");
            }
#endif
            var idx = Array.IndexOf(_table.Columns, column);
            return ((IColumn<T>)_table._columnData)[idx];
        }

        public T Get<T>(int idx)
        {
            return ((IColumn<T>)_table._columnData[idx])[idx];
        }
    }

    public void AddFrom(TableJoiner.JoinerEnumerator joinerEnumerator, int src, int dest)
    {
        ((IAppendableColumn)_columnData[dest]).AddFrom(joinerEnumerator, src);
    }
}
