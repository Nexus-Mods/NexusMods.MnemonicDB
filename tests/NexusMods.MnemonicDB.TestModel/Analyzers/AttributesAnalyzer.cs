using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.TestModel.Analyzers;

/// <summary>
/// Records all the attributes in each transaction
/// </summary>
public class AttributesAnalyzer : IAnalyzer<HashSet<IAttribute>>
{
    public object Analyze(IDb db)
    {
        var hashSet = new HashSet<IAttribute>();
        var registry = db.Registry;
        foreach (var datom in db.RecentlyAdded)
        {
            hashSet.Add(registry.GetAttribute(datom.A));
        }

        return hashSet;
    }
}
