using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Query.Abstractions;
using NexusMods.Query.Abstractions.Engines.Abstract;
using NexusMods.Query.Tests.Models;


namespace NexusMods.Query.Tests;


public class SimpleDBTests(IServiceProvider provider) : ASolarSystemTest(provider)
{
    /*
    public Func<IDb, string, List<string>> SystemsConnectedTo =
        QueryBuilder.New<string>(out var fromName)
            .Declare<EntityId>(out var fromId)
            .Declare<EntityId>(out var toId)
            .Declare<string>(out var toName)
            .Datoms(fromId, SolarSystem.Name, fromName)
            .Datoms(fromId, SolarSystem.JumpsOut, toId)
            .Datoms(toId, SolarSystem.Name, toName)
            .Return<string, string>(toName);
    
    [Fact]
    public async Task SystemsConnectedToJita()
    {
        var results = SystemsConnectedTo(Connection.Db, "Jita");
        await Verify(results);
    }
    */

    public Func<IDb, List<string>> BorderSystemsQuery = 
        QueryBuilder.New()
            .Declare<EntityId>(out var lowSystem, out var highSystem, out var nullSystem)
            .Declare<string>(out var lowName, out var highName, out var nullName)
            .Datoms(lowSystem, SolarSystem.SecurityStatus, SecurityClass.Low)
            .Datoms(lowSystem, SolarSystem.Name, lowName)
            .Datoms(lowSystem, SolarSystem.JumpsOut, highSystem)
            .Datoms(highSystem, SolarSystem.SecurityStatus, SecurityClass.High)
            .Datoms(nullSystem, SolarSystem.JumpsOut, lowSystem)
            .Datoms(nullSystem, SolarSystem.SecurityStatus, SecurityClass.Null)
            .Datoms(highSystem, SolarSystem.Name, highName)
            .Datoms(nullSystem, SolarSystem.Name, nullName)
            .Return(lowName); 
    
    [Fact]
    public async Task BorderSystems()
    {
        var sw = Stopwatch.StartNew();
        var results = BorderSystemsQuery(Connection.Db);
        Logger.LogInformation($"Query took {sw.ElapsedMilliseconds}ms, returned {results.Count}");

        //await Verify(results);
    }
}
