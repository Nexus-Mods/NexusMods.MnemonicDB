using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using NexusMods.Query.Tests.Models;

namespace NexusMods.Query.Tests;

public class ASolarSystemTest : IAsyncLifetime
{
    protected readonly IConnection Connection;
    protected readonly ILogger Logger;

    public ASolarSystemTest(IServiceProvider conn)
    {
        Connection = conn.GetRequiredService<IConnection>();
        Logger = conn.GetRequiredService<ILogger<ASolarSystemTest>>();
    }
    
    public async Task InitializeAsync()
    {
        using var tx = Connection.BeginTransaction();
        
        Dictionary<ulong, EntityId> mappings = new();
        
        foreach (var solarSystem in await ReadCSVFile("mapSolarSystems.csv.gz"))
        {
            var security = float.Parse(solarSystem["security"]);
            var securityClass = security switch
            {
                >= 0.5f => SecurityClass.High,
                > 0.0f => SecurityClass.Low,
                _ => SecurityClass.Null,
            };

            var system = new SolarSystem.New(tx)
            {
                Name = solarSystem["solarSystemName"],
                SecurityLevel = security,
                SecurityClass = solarSystem["securityClass"],
                SecurityStatus = securityClass,
            };
            
            mappings[ulong.Parse(solarSystem["solarSystemID"])] = system.Id;
        }
        
        foreach (var jump in await ReadCSVFile("mapSolarSystemJumps.csv.gz"))
        {
            var from = mappings[ulong.Parse(jump["fromSolarSystemID"])];
            var to = mappings[ulong.Parse(jump["toSolarSystemID"])];
            tx.Add(from, SolarSystem.JumpsOut, to);
        }
        
        await tx.Commit();
    }

    private async Task<List<Dictionary<string, string>>> ReadCSVFile(RelativePath file)
    {
        var fullPath = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources").Combine(file);
        await using var stream = new GZipStream(fullPath.Read(), CompressionMode.Decompress, false);
        using var reader = new StreamReader(stream);
        var lines = (await reader.ReadToEndAsync()).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        
        var names = lines[0].Split(',', StringSplitOptions.TrimEntries);
        var data = new List<Dictionary<string, string>>();
        
        foreach (var line in lines.Skip(1))
        {
            var values = line.Split(',', StringSplitOptions.TrimEntries);
            var dict = new Dictionary<string, string>();
            for (var i = 0; i < names.Length; i++)
            {
                dict[names[i]] = values[i];
            }
            data.Add(dict);
        }

        return data;
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return Task.CompletedTask;
    }

}
