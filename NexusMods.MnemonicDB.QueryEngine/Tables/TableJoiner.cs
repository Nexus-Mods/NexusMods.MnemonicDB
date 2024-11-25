using System;

namespace NexusMods.MnemonicDB.QueryEngine.Tables;

/// <summary>
/// This facilitates joining two tables together. To start the class is provided
/// a source table and columns to select and copy from the source table, as well
/// as new rows that will be added to the output. The class then provides an
/// enumerable interface where users can go through the source rows one at a time
/// copy zero or more rows to the output table. Columns marked as "copy" are
/// copied and duplicated in the output if multiple rows are emitted.
/// </summary>
public class TableJoiner
{
    private readonly LVar[] _inputColumns;
    private readonly (int Src, int Dest)[] _copyColumns;
    private readonly (int Src, int Dest)[] _joinColumns;
    private readonly int[] _newRows;
    private readonly LVar[] _outputColumns;

    public TableJoiner(LVar[] inputColumns, (int Src, int Dest)[] copyColumns, (int Src, int Dest)[] joinColumns, int[] newColumns, LVar[] outputColumns)
    {
        _inputColumns = inputColumns;
        _copyColumns = copyColumns;
        _joinColumns = joinColumns;
        _newRows = newColumns;
        _outputColumns = outputColumns;
    }
    
    public JoinerEnumerator GetEnumerator(ITable src)
    {
        return new JoinerEnumerator(this, src);
    }
    
    public ref struct JoinerEnumerator
    {
        private readonly ITable _src;
        private IRowEnumerator? _srcEnumerator;
        private readonly AppendableTable _dest;
        private readonly TableJoiner _joiner;

        public JoinerEnumerator(TableJoiner tableJoiner, ITable src)
        {
            _src = src;
            _joiner = tableJoiner;
            _dest = new AppendableTable(tableJoiner._outputColumns);
        }
        
        /// <summary>
        /// Gets the value of the column at the given index from the source table
        /// </summary>
        public T Get<T>(int column)
        {
            return _srcEnumerator!.Get<T>(column);
        }
        
        public bool MoveNext()
        {
            if (_srcEnumerator == null)
            {
                _srcEnumerator = _src.EnumerateRows();
            }
            return _srcEnumerator.MoveNext();
        }

        public void FinishRow()
        {
            foreach (var (src, dest) in _joiner._copyColumns)
            {
                if (dest == -1) continue;
                _dest.AddFrom(this, src, dest);
            }
            foreach (var (src, dest) in _joiner._joinColumns)
            {
                if (dest == -1) continue;
                _dest.AddFrom(this, src, dest);
            }
            _dest.FinishRow();
        }
        
        public ITable FinishTable()
        {
            _dest.Freeze();
            return _dest;
        }

        public void Add<T>(int emitColumn, T dest)
        {
            ((IAppendableColumn<T>)_dest[emitColumn]).Add(dest);
        }
    }
}
