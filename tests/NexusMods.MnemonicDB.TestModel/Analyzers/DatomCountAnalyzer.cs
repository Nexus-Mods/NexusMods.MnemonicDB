using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.TestModel.Analyzers;

/// <summary>
/// Counts the number of dataoms in each transaction
/// </summary>
public class DatomCountAnalyzer : IAnalyzer<int>
{
    public object Analyze(IDb? dbOld, IDb dbNew)
    {
        return dbNew.RecentlyAdded.Count;
    }
}
