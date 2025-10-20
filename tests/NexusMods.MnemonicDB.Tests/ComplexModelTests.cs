using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

[WithServiceProvider]
public class ComplexModelTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{

    [Test]
    [Arguments(1, 1)]
    [Arguments(1, 16)]
    [Arguments(16, 1)]
    [Arguments(16, 16)]
    [Arguments(16, 128)]
    [Arguments(128, 16)]
    [Arguments(128, 128)]
    [Arguments(1024, 128)]
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
                    Hash = HashAsUtf8(name)
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


        var loadoutRO = result.Remap(loadout);

        await Assert.That(loadoutRO.Mods.Count).IsEqualTo(modCount).Because("all mods should be loaded");

        await Assert.That(loadoutRO.Collections.Count).IsEqualTo(2).Because("all collections should be loaded");

        await Assert.That(loadoutRO.Collections.SelectMany(c => c.ModIds).Count())
            .IsEqualTo(loadoutRO.Mods.Count())
            .Because("all mods should be in a collection");

        sw.Restart();
        
        foreach (var mod in loadoutRO.Mods)
            //totalSize += mod.Files.Sum(f => f.Size);
            await Assert.That(mod.Files.Count)
                .IsEqualTo(filesPerMod)
                .Because("every mod should have the same amount of files");


        //totalSize.Should().BeGreaterThan(Size.FromLong(modCount * filesPerMod * "File ".Length), "total size should be the sum of all file sizes");

        Logger.LogInformation(
            $"Loadout: {loadoutRO.Name} ({modCount * filesPerMod} entities) loaded in {sw.ElapsedMilliseconds}ms");

    }


    [Test]
    [Arguments(1, 1, 1)]
    [Arguments(1, 16, 16)]
    [Arguments(16, 1, 1)]
    [Arguments(16, 16, 16)]
    [Arguments(16, 128, 128)]
    [Arguments(128, 16, 16)]
    [Arguments(128, 128, 128)]
    [Arguments(1024, 128, 128)]
    [Arguments(128, 1024, 128)]
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
                    Hash = HashAsUtf8(name)
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
                Hash = HashAsUtf8(name)
            };

            files.Add(file);
        }

        await extraTx.Commit();

        Logger.LogInformation("Restarting storage");
        await Assert.That(Connection.Db.RecentlyAdded.ToArray()).IsNotEmpty().Because("the last transaction added data");

        var lastTxId = Connection.TxId;
        await RestartDatomStore();

        await Assert.That(Connection.TxId).IsEqualTo(lastTxId)
            .Because("the transaction id should be the same after a restart");
       
        
        
        await Assert.That(Connection.Db.BasisTxId).IsEqualTo(lastTxId)
            .Because("the basis transaction id should be the same after a restart");
        
        await Assert.That(Connection.Db.RecentlyAdded.ToArray()).IsNotEmpty()
            .Because("the restarted database should populate the recently added");
        
        Logger.LogInformation("Storage restarted");


        loadout = loadout.Rebase(Connection.Db);

        var totalSize = Size.Zero;

        await Assert.That(loadout.Mods.Count).IsEqualTo(modCount)
            .Because("all mods should be loaded");
        
        foreach (var mod in loadout.Mods)
        {
            totalSize += mod.Files.Sum(f => f.Size);

            if (mod.Id == firstMod.Id)
                await Assert.That(mod.Files.Count).IsEqualTo(filesPerMod + extraFiles)
                    .Because("first mod should have the extra files");
            else
                await Assert.That(mod.Files.Count).IsEqualTo(filesPerMod)
                    .Because("every mod should have the same amount of files");
        }

        using var tx2 = Connection.BeginTransaction();
        var newNewLoadOutNew = new Loadout.New(tx2)
        {
            Name = "My Loadout 2"
        };

        var result2 = await tx2.Commit();
        var newNewLoadOut = result2.Remap(newNewLoadOutNew);

        await Assert.That(newNewLoadOut.Id).IsNotEqualTo(loadout.Id)
            .Because("new loadout should have a different id because the connection re-detected the max EntityId");
    }

    [Test]
    public async Task CanGetFromTransaction()
    {
        using var tx = Connection.BeginTransaction();

        var archiveFile = new ArchiveFile.New(tx, out var id)
        {
            Hash = Hash.Zero,
            Path = "foo",
            File = new File.New(tx, id)
            {
                Hash = Hash.Zero,
                Path = "foo",
                Size = Size.One,
                ModId = tx.TempId(),
            },
        };

        await Assert.That(ArchiveFile.Load(tx.AsIf(), archiveFile).Path).IsEqualTo("foo");
        archiveFile.Path = "bar";
        await Assert.That(ArchiveFile.Load(tx.AsIf(), archiveFile).Path).IsEqualTo("bar");

        var result = await tx.Commit();
        var remap = result.Remap(archiveFile);
        await Assert.That(remap.AsFile().Path).IsEqualTo("bar");
    }

    private static Hash HashAsUtf8(string value) => Hash.FromLong(Encoding.UTF8.GetBytes(value).xxHash3());
}
