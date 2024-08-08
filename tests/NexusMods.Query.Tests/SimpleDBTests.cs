using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Query.Abstractions;
using NexusMods.Query.Abstractions.Facts;

namespace NexusMods.Query.Tests;

public class SimpleDBTests : IAsyncLifetime
{
    private readonly IConnection _conn;
    public SimpleDBTests(IConnection conn)
    {
        _conn = conn;
    }
    
    public Func<IDb, IEnumerable<(EntityId, string)>> GetMyFacts = 
        Query.Abstractions.Query
        .New()
        .Datom(out var modId, Mod.Name, "Test Mod")
        .Datom(modId, Mod.Loadout, out var loadoutId)
        .Datom(loadoutId, Loadout.Name, out var name)
        .ToQuery(loadoutId, name);


    [Fact]
    public async Task Test1()
    {
        var results = GetMyFacts(_conn.Db).ToArray();


        await Verify(results);
    }

    public async Task InitializeAsync()
    {
        using var tx = _conn.BeginTransaction();
        var loadout = new Loadout.New(tx)
        {
            Name = "Test Loadout",
        };
        
        var mod = new Mod.New(tx)
        {
            Name = "Test Mod",
            LoadoutId = loadout.Id,
            Source = new Uri("https://www.nexusmods.com"),
        };
        
        await tx.Commit();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

public static class Extensions
{
    


}
