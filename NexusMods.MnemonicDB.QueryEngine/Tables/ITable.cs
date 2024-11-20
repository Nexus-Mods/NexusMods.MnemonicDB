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
    /// Get the column with the given name
    /// </summary>
    public IColumn this[LVar column] { get; }
    
    /// <summary>
    /// Get the column at the given index
    /// </summary>
    public IColumn this[int idx] { get; }
}
