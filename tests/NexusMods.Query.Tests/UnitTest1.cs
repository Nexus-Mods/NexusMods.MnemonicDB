using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Query.Abstractions;
using NexusMods.Query.Abstractions.Facts;

namespace NexusMods.Query.Tests;

public class UnitTest1
{
    public Func<IDb, IEnumerable<LoadoutNames>> GetMyFacts = 
        Query.Abstractions.Query
        .New()
        .Datom(out var modId, Mod.Name, "Test")
        .Datom(modId, Mod.Loadout, out var loadoutId)
        .Datom(loadoutId, Loadout.Name, out var name)
        .MyFact(loadoutId, name);
    
    [Fact]
    public void Test1()
    {
        

    }

    
    public record struct LoadoutNames(EntityId LoadoutId, string Name) : IFact<EntityId, string>;
}

public static class Extensions
{
    


}
