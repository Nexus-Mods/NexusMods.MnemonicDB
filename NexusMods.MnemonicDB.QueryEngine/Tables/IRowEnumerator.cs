namespace NexusMods.MnemonicDB.QueryEngine.Tables;

public interface IRowEnumerator
{
    /// <summary>
    /// Move to the next row in the table, returning false if there are no more rows
    /// </summary>
    public bool MoveNext();
    
    /// <summary>
    /// Get the columns of the table
    /// </summary>
    public LVar[] Columns { get; }
    
    /// <summary>
    /// Get the value of the column at the current row
    /// </summary>
    public T Get<T>(LVar column);
    
    /// <summary>
    /// Get the value of the column at the current row
    /// </summary>
    public T Get<T>(int idx);
}
