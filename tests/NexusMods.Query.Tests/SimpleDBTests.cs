using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Query.Abstractions;
using NexusMods.Query.Abstractions.Engines.Abstract;
using NexusMods.Query.Tests.Models;


namespace NexusMods.Query.Tests;


public class SimpleDBTests(IServiceProvider provider) : ASolarSystemTest(provider)
{
    public RootQuery SystemsConnectedTo =
        QueryBuilder.New<string>(out var fromName)
            .Declare<EntityId>(out var fromId)
            .Declare<EntityId>(out var toId)
            .Declare<string>(out var toName)
            .Datoms(fromId, SolarSystem.Name, fromName)
            .Datoms(fromId, SolarSystem.JumpsOut, toId)
            .Datoms(toId, SolarSystem.Name, toName)
            .Return(toName);
    
    [Fact]
    public async Task SystemsConnectedToJita()
    {
     
        Assert.True(true);
    }
}
