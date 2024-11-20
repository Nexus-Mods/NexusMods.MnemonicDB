namespace NexusMods.MnemonicDB.QueryEngine.Tables;

/// <summary>
/// A table is a collection of columns and rows of typed data. The columns
/// are named via LVars and the rows are accessed by index. Since data is stored
/// in a columnar format, and columns are abstract, there is room here for memory
/// optimizations by using columnar storage lightweight compression (bit packing,
/// typed arrays, etc).
/// </summary>
public interface ITable
{
    /// <summary>
    /// The columns in this table
    /// </summary>
    public LVar[] Columns { get; }
    
    /// <summary>
    /// The number of rows in this table
    /// </summary>
    public int Count { get; }
    
    /// <summary>
    /// Get the column with the given name
    /// </summary>
    public IColumn this[LVar column] { get; }
    
    /// <summary>
    /// Get the column at the given index
    /// </summary>
    public IColumn this[int idx] { get; }

    /// <summary>
    /// Gets an enumerator for the rows in the table
    /// </summary>
    public IRowEnumerator EnumerateRows();
}
