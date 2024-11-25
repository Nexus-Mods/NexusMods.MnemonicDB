namespace NexusMods.MnemonicDB.QueryEngine.Tables;

/// <summary>
/// A interface for a column that is materialized (can be accessed by row index).
/// </summary>
public interface IMaterializedColumn
{
    /// <summary>
    /// Get the hash code for the cell at the given row.
    /// </summary>
    public int GetHashCode(int row);

    /// <summary>
    /// Get the number of rows in the column.
    /// </summary>
    public int Count { get; }
}
