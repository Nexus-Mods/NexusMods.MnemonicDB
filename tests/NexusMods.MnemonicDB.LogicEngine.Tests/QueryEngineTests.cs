using NexusMods.MnemonicDB.Abstractions;
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

        var query = Query.New<EntityId>(out var loadout)
            .Declare<EntityId>(out var mod)
            .Declare<string>(out var modName)
            .Where(Datoms(mod, Mod.Loadout, loadout),
                   Datoms(mod, Mod.Name, modName))
            .Return(modName);
        

        var db = Connection.Db;
        query.Run(db, inserted.Id)
            .Should()
            .BeEquivalentTo(["Mod1 - Updated", "Mod2 - Updated", "Mod3 - Updated"]);
    }
    
}
