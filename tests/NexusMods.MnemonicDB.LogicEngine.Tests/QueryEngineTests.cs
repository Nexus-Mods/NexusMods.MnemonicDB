using NexusMods.MnemonicDB.TestModel;
using NexusMods.MnemonicDB.Tests;
using static NexusMods.MnemonicDB.LogicEngine.QueryBuilder;

namespace NexusMods.MnemonicDB.LogicEngine.Tests;

public class QueryEngineTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{

    [Fact]
    public async Task CanQueryForChildren()
    {
        var inserted = await InsertExampleData();
        var engine = new QueryEngine();
        var mod = LVar.Create("mod");
        var modName = LVar.Create("modName");

        var query =
            And(
                Datoms(mod, Mod.Loadout, inserted.LoadoutId),
                Datoms(mod, Mod.Name, modName)
            );

        var db = Connection.Db;
        var results = engine.QueryAll<string>(db, query, modName);
    }
    
}
