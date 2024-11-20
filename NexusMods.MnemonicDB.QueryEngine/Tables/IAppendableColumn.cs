namespace NexusMods.MnemonicDB.QueryEngine.Tables;

public interface IAppendableColumn
{
    /// <summary>
    /// Add a value to the column
    /// </summary>
    public void AddFrom(TableJoiner.JoinerEnumerator e, int srcColumn);
}
public interface IAppendableColumn<in T> : IAppendableColumn
{
    /// <summary>
    /// Add a value to the column
    /// </summary>
    public void Add(T value);
}
