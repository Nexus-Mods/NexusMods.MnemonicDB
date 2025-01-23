using System.IO.Compression;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.MnemonicDB.Tests.Resources;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

public class QueryTests(IServiceProvider provider) : AMnemonicDBTest(provider), IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        var flow = new Flow();

        await using var fs = (FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory) /
                              "Resources/SmallModlist.json.gz").Read();
        await using var gz = new GZipStream(fs, CompressionMode.Decompress);
        var json = await JToken.LoadAsync(new JsonTextReader(new StreamReader(gz)));

        var tx = Connection.BeginTransaction();

        var loadout = new Loadout.New(tx)
        {
            Name = "Test Loadout",
        };
        
        flow.Update(ops => { ops.AddData(ModlistParser.ParsedToken, 1, json); });

        Dictionary<string, EntityId> modIds = new();
        foreach (var (_, modName) in flow.Query(ModlistParser.DirectiveWithModName))
        {
            if (modIds.ContainsKey(modName))
                continue;
            
            var newMod = new Mod.New(tx)
            {
                Name = modName,
                Source = new Uri("source://" + modName.xxHash3AsUtf8()),
                LoadoutId = loadout
            };
            
            modIds.Add(modName, newMod.Id);
        }
        
        foreach (var (directive, modName) in flow.Query(ModlistParser.DirectiveWithModName))
        {
            _ = new File.New(tx)
            {
                Path = directive.To,
                ModId = modIds[modName],
                Hash = directive.Hash,
                Size = directive.Size,
            };
        }
        
        await tx.Commit();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CanSelectModels()
    {
        var db = Connection.Db;

        var q = from file in File.All()
            select file;

        var results = db.Query(q);

        results.Count.Should().Be(46176);
    }
}
