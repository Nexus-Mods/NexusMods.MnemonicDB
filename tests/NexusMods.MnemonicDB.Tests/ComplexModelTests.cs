using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash3;
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

        loadoutRO.Mods.Count.Should().Be(modCount, "all mods should be loaded");

        loadoutRO.Collections.Count.Should().Be(2, "all collections should be loaded");

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
        Connection.Db.RecentlyAdded.Should().NotBeEmpty("the last transaction added data");

        var lastTxId = Connection.TxId;
        await RestartDatomStore();
        
        Connection.TxId.Should().Be(lastTxId, "the transaction id should be the same after a restart");
        Connection.Db.BasisTxId.Should().Be(lastTxId, "the basis transaction id should be the same after a restart");
        
        Connection.Db.RecentlyAdded.Should().NotBeEmpty("the restarted database should populate the recently added");
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

    [Fact]
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

        archiveFile.GetFile(tx).Path.Should().Be("foo");
        archiveFile.GetFile(tx).Path = "bar";
        archiveFile.GetFile(tx).Path.Should().Be("bar");

        var result = await tx.Commit();
        var remap = result.Remap(archiveFile);
        remap.AsFile().Path.Should().Be("bar");
    }

    private static Hash HashAsUtf8(string value) => Hash.FromLong(Encoding.UTF8.GetBytes(value).xxHash3());
}
