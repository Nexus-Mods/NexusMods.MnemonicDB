using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

public class ComplexModelTests(IServiceProvider provider) : AMnemonicDBTest(provider)
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

        var tx = Connection.BeginTransaction();

        var loadout = new Loadout.New(tx)
        {
            Name = "My Loadout"
        };

        var mods = new List<Mod.New>();
        var files = new List<File.New>();

        for (var i = 0; i < modCount; i++)
        {
            var mod = new Mod.New(tx)
            {
                Name = $"Mod {i}",
                Source = new Uri($"http://mod{i}.com"),
                LoadoutId = loadout
            };

            mods.Add(mod);
            for (var j = 0; j < filesPerMod; j++)
            {
                var name = $"File {j}";

                var file = new File.New(tx)
                {
                    Path = name,
                    ModId = mod,
                    Size = Size.FromLong(name.Length),
                    Hash = Hash.FromLong(name.XxHash64AsUtf8())
                };

                files.Add(file);
            }
        }

        var oddCollection = new Collection.New(tx)
        {
            Name = "Odd Mods",
            ModIds = mods.Where((m, idx) => idx % 2 == 1).Select(m => m.Id).ToArray(),
            LoadoutId = loadout
        };

        var evenCollection = new Collection.New(tx)
        {
            Name = "Even Mods",
            ModIds = mods.Where((m, idx) => idx % 2 == 0).Select(m => m.Id).ToArray(),
            LoadoutId = loadout
        };

        var sw = Stopwatch.StartNew();
        var result = await tx.Commit();
        Logger.LogInformation($"Commit took {sw.ElapsedMilliseconds}ms");


        var db = Connection.Db;

        var loadoutRO = result.Remap(loadout);

        var totalSize = Size.Zero;

        loadoutRO.Mods.Count().Should().Be(modCount, "all mods should be loaded");

        loadoutRO.Collections.Count().Should().Be(2, "all collections should be loaded");

        loadoutRO.Collections.SelectMany(c => c.ModIds)
            .Count().Should().Be(loadoutRO.Mods.Count(), "all mods should be in a collection");

        sw.Restart();
        foreach (var mod in loadoutRO.Mods)
            //totalSize += mod.Files.Sum(f => f.Size);
            mod.Files.Count().Should().Be(filesPerMod, "every mod should have the same amount of files");


        //totalSize.Should().BeGreaterThan(Size.FromLong(modCount * filesPerMod * "File ".Length), "total size should be the sum of all file sizes");

        Logger.LogInformation(
            $"Loadout: {loadout.Name} ({modCount * filesPerMod} entities) loaded in {sw.ElapsedMilliseconds}ms");

    }


    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 16, 16)]
    [InlineData(16, 1, 1)]
    [InlineData(16, 16, 16)]
    [InlineData(16, 128, 128)]
    [InlineData(128, 16, 16)]
    [InlineData(128, 128, 128)]
    [InlineData(1024, 128, 128)]
    [InlineData(128, 1024, 128)]
    public async Task CanRestartStorage(int modCount, int filesPerMod, int extraFiles)
    {
        using var tx = Connection.BeginTransaction();

        var newLoadout = new Loadout.New(tx)
        {
            Name = "My Loadout"
        };

        var mods = new List<Mod.New>();
        var files = new List<File.New>();

        for (var i = 0; i < modCount; i++)
        {
            var mod = new Mod.New(tx)
            {
                Name = $"Mod {i}",
                Source = new Uri($"http://mod{i}.com"),
                LoadoutId = newLoadout
            };

            mods.Add(mod);
            for (var j = 0; j < filesPerMod; j++)
            {
                var name = $"File {j}";

                var file = new File.New(tx)
                {
                    Path = name,
                    ModId = mod,
                    Size = Size.FromLong(name.Length),
                    Hash = Hash.FromLong(name.XxHash64AsUtf8())
                };

                files.Add(file);
            }
        }

        var result = await tx.Commit();

        var extraTx = Connection.BeginTransaction();
        var loadout = result.Remap(newLoadout);

        var firstMod = result.Remap(mods[0]);
        for (var idx = 0; idx < extraFiles; idx++)
        {
            var name = $"Extra File {idx}";

            var file = new File.New(extraTx)
            {
                Path = name,
                ModId = firstMod,
                Size = Size.FromLong(name.Length),
                Hash = Hash.FromLong(name.XxHash64AsUtf8())
            };

            files.Add(file);
        }

        await extraTx.Commit();

        Logger.LogInformation("Restarting storage");
        await RestartDatomStore();
        Logger.LogInformation("Storage restarted");


        loadout = loadout.Rebase(Connection.Db);

        var totalSize = Size.Zero;

        loadout.Mods.Count().Should().Be(modCount, "all mods should be loaded");
        foreach (var mod in loadout.Mods)
        {
            totalSize += mod.Files.Sum(f => f.Size);

            if (mod.Id == firstMod.Id)
                mod.Files.Count.Should().Be(filesPerMod + extraFiles, "first mod should have the extra files");
            else
                mod.Files.Count.Should().Be(filesPerMod, "every mod should have the same amount of files");
        }

        using var tx2 = Connection.BeginTransaction();
        var newNewLoadOutNew = new Loadout.New(tx2)
        {
            Name = "My Loadout 2"
        };

        var result2 = await tx2.Commit();
        var newNewLoadOut = result2.Remap(newNewLoadOutNew);

        newNewLoadOut.Id.Should().NotBe(loadout.Id,
            "new loadout should have a different id because the connection re-detected the max EntityId");
    }
}
