using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.QueryEngine;

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
