using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash64;
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
        throw new NotImplementedException();
        /*
    var tx = Connection.BeginTransaction();

    var loadout = new Loadout.Model(tx)
    {
        Name = "My Loadout"
    };

    var oddCollection = new Collection.Model(tx)
    {
        Name = "Odd Mods",
        Loadout = loadout
    };

    var evenCollection = new Collection.Model(tx)
    {
        Name = "Even Mods",
        Loadout = loadout
    };

    var mods = new List<Mod.Model>();
    var files = new List<File.Model>();

    for (var i = 0; i < modCount; i++)
    {
        var mod = new Mod.Model(tx)
        {
            Name = $"Mod {i}",
            Source = new Uri($"http://mod{i}.com"),
            Loadout = loadout
        };

        if (i % 2 == 0)
            evenCollection.Attach(mod);
        else
            oddCollection.Attach(mod);

        mods.Add(mod);
        for (var j = 0; j < filesPerMod; j++)
        {
            var name = $"File {j}";

            var file = new File.Model(tx)
            {
                Path = name,
                Mod = mod,
                Size = Size.FromLong(name.Length),
                Hash = Hash.FromLong(name.XxHash64AsUtf8())
            };

            files.Add(file);
        }
    }

    var sw = new Stopwatch();
    var result = await tx.Commit();
    Logger.LogInformation($"Commit took {sw.ElapsedMilliseconds}ms");


    var db = Connection.Db;

    loadout = db.Get<Loadout.Model>(result[loadout.Id]);

    var totalSize = Size.Zero;

    loadout.Mods.Count().Should().Be(modCount, "all mods should be loaded");

    loadout.Collections.Count().Should().Be(2, "all collections should be loaded");

    loadout.Collections.SelectMany(c => c.Mods)
        .Count().Should().Be(loadout.Mods.Count(), "all mods should be in a collection");

    sw.Restart();
    foreach (var mod in loadout.Mods)
        //totalSize += mod.Files.Sum(f => f.Size);
        mod.Files.Count().Should().Be(filesPerMod, "every mod should have the same amount of files");


    //totalSize.Should().BeGreaterThan(Size.FromLong(modCount * filesPerMod * "File ".Length), "total size should be the sum of all file sizes");

    Logger.LogInformation(
        $"Loadout: {loadout.Name} ({modCount * filesPerMod} entities) loaded in {sw.ElapsedMilliseconds}ms");
        */
    }

    /*
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

    var loadout = new Loadout.Model(tx)
    {
        Name = "My Loadout"
    };

    var mods = new List<Mod.Model>();
    var files = new List<File.Model>();

    for (var i = 0; i < modCount; i++)
    {
        var mod = new Mod.Model(tx)
        {
            Name = $"Mod {i}",
            Source = new Uri($"http://mod{i}.com"),
            Loadout = loadout
        };

        mods.Add(mod);
        for (var j = 0; j < filesPerMod; j++)
        {
            var name = $"File {j}";

            var file = new File.Model(tx)
            {
                Path = name,
                Mod = mod,
                Size = Size.FromLong(name.Length),
                Hash = Hash.FromLong(name.XxHash64AsUtf8())
            };

            files.Add(file);
        }
    }

    var result = await tx.Commit();

    var extraTx = Connection.BeginTransaction();

    var db = Connection.Db;
    var firstMod = db.Get<Mod.Model>(result.Remap(mods[0]).Id);
    for (var idx = 0; idx < extraFiles; idx++)
    {
        var name = $"Extra File {idx}";

        var file = new File.Model(extraTx)
        {
            Path = name,
            Mod = firstMod,
            Size = Size.FromLong(name.Length),
            Hash = Hash.FromLong(name.XxHash64AsUtf8())
        };

        files.Add(file);
    }

    await extraTx.Commit();

    Logger.LogInformation("Restarting storage");
    await RestartDatomStore();
    Logger.LogInformation("Storage restarted");

    db = Connection.Db;

    loadout = db.Get<Loadout.Model>(result[loadout.Id]);

    var totalSize = Size.Zero;

    loadout.Mods.Count().Should().Be(modCount, "all mods should be loaded");
    foreach (var mod in loadout.Mods)
    {
        totalSize += mod.Files.Sum(f => f.Size);

        if (mod.Id == firstMod.Id)
            mod.Files.Count().Should().Be(filesPerMod + extraFiles, "first mod should have the extra files");
        else
            mod.Files.Count().Should().Be(filesPerMod, "every mod should have the same amount of files");
    }

    using var tx2 = Connection.BeginTransaction();
    var newLoadOut = new Loadout.Model(tx2)
    {
        Name = "My Loadout 2"
    };

    var result2 = await tx2.Commit();
    newLoadOut = db.Get<Loadout.Model>(result2[newLoadOut.Id]);

    newLoadOut.Id.Should().NotBe(loadout.Id,
        "new loadout should have a different id because the connection re-detected the max EntityId");
}
*/
}
