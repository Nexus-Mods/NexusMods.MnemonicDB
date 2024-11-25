namespace NexusMods.MnemonicDB.QueryEngine.Tables;

public interface IMaterializedTable : ITable
{
    public int Count { get; }
}
