using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.ComplexModel.ReadModels;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using File = NexusMods.EventSourcing.TestModel.ComplexModel.ReadModels.File;

namespace NexusMods.EventSourcing.Tests;

public class ComplexModelTests(IServiceProvider provider) : AEventSourcingTest(provider)
{

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 16)]
    [InlineData(16, 1)]
    [InlineData(16, 16)]
    [InlineData(16, 128)]
    [InlineData(128, 16)]
    [InlineData(128, 128)]
    [InlineData(1024, 128)]
    public async Task CanCreateLoadout(int modCount, int filesPerMod)
    {
        Config.MaxInMemoryDatoms = 1024;
        var tx = Connection.BeginTransaction();

        var loadout = Loadout.Create(tx, "My Loadout");

        var mods = new List<Mod>();
        var files = new List<File>();

        for (var i = 0; i < modCount; i++)
        {
            var mod = Mod.Create(tx, $"Mod {i}", new Uri($"http://mod{i}.com"), loadout);
            mods.Add(mod);
            for (var j = 0; j < filesPerMod; j++)
            {
                var name = $"File {j}";
                var file = File.Create(tx, name, mod, Size.FromLong(name.Length), Hash.FromLong(name.XxHash64AsUtf8()));
                files.Add(file);
            }
        }

        var sw = new Stopwatch();
        var result = await tx.Commit();
        Logger.LogInformation($"Commit took {sw.ElapsedMilliseconds}ms");

        sw.Restart();

        var db = Connection.Db;

        loadout =  db.Get<Loadout>(result[loadout.Id]);

        var totalSize = Size.Zero;

        loadout.Mods.Count().Should().Be(modCount, "all mods should be loaded");
        foreach (var mod in loadout.Mods)
        {
            totalSize += mod.Files.Sum(f => f.Size);
            mod.Files.Count().Should().Be(filesPerMod, "every mod should have the same amount of files");
        }


        totalSize.Should().BeGreaterThan(Size.FromLong(modCount * filesPerMod * "File ".Length), "total size should be the sum of all file sizes");

        Logger.LogInformation($"Loadout: {loadout.Name} ({modCount * filesPerMod} entities) loaded in {sw.ElapsedMilliseconds}ms");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 16)]
    [InlineData(16, 1)]
    [InlineData(16, 16)]
    [InlineData(16, 128)]
    [InlineData(128, 16)]
    [InlineData(128, 128)]
    [InlineData(1024, 128)]
    [InlineData(128, 1024)]
    public async Task CanRestartStorage(int modCount, int filesPerMod)
    {
        var tx = Connection.BeginTransaction();

        var loadout = Loadout.Create(tx, "My Loadout");

        var mods = new List<Mod>();
        var files = new List<File>();

        for (var i = 0; i < modCount; i++)
        {
            var mod = Mod.Create(tx, $"Mod {i}", new Uri($"http://mod{i}.com"), loadout);
            mods.Add(mod);
            for (var j = 0; j < filesPerMod; j++)
            {
                var name = $"File {j}";
                var file = File.Create(tx, name, mod, Size.FromLong(name.Length), Hash.FromLong(name.XxHash64AsUtf8()));
                files.Add(file);
            }
        }

        var result = await tx.Commit();

        Logger.LogInformation("Restarting storage");
        await RestartDatomStore();
        Logger.LogInformation("Storage restarted");

        var db = Connection.Db;

        loadout =  db.Get<Loadout>(result[loadout.Id]);

        var totalSize = Size.Zero;

        loadout.Mods.Count().Should().Be(modCount, "all mods should be loaded");
        foreach (var mod in loadout.Mods)
        {
            totalSize += mod.Files.Sum(f => f.Size);
            mod.Files.Count().Should().Be(filesPerMod, "every mod should have the same amount of files");
        }

        tx = Connection.BeginTransaction();
        var newLoadOut = Loadout.Create(tx, "My Loadout 2");
        var result2 = await tx.Commit();
        newLoadOut = db.Get<Loadout>(result2[newLoadOut.Id]);

        newLoadOut.Id.Should().NotBe(loadout.Id, "new loadout should have a different id because the connection re-detected the max EntityId");

    }



}
