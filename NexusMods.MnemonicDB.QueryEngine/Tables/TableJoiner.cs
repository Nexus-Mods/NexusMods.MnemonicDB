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

    public TableJoiner(LVar[] inputColumns, (int Src, int Dest)[] copyColumns, (int Src, int Dest)[] joinColumns, int[] newRows, LVar[] outputColumns)
    {
        _inputColumns = inputColumns;
        _copyColumns = copyColumns;
        _joinColumns = joinColumns;
        _newRows = newRows;
        _outputColumns = outputColumns;
    }
    
    public JoinerEnumerator GetEnumerator(ITable src)
    {
        return new JoinerEnumerator(this, src);
    }
    
    public ref struct JoinerEnumerator
    {
        private readonly ITable _src;
        public JoinerEnumerator(TableJoiner tableJoiner, ITable src)
        {
            _src = src;
        }

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        public void FinishRow()
        {
            
        }
        
        public ITable FinishTable()
        {
            throw new NotImplementedException();
        }
    }
}
