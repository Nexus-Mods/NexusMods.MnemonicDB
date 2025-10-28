using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.TestModel.Analyzers;

/// <summary>
/// Records all the attributes in each transaction
/// </summary>
public class AttributesAnalyzer : IAnalyzer<HashSet<Symbol>>
{
    public object Analyze(IDb? dbOld, IDb dbNew)
    {
        var hashSet = new HashSet<Symbol>();
        var cache = dbNew.AttributeResolver.AttributeCache;
        foreach (var datom in dbNew.RecentlyAdded)
        {
            hashSet.Add(cache.GetSymbol(datom.A));
        }
        return hashSet;
    }
}
