using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using TUnit.Assertions.AssertConditions.Throws;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

[WithServiceProvider]
public class MigrationTests : AMnemonicDBTest
{
    public MigrationTests(IServiceProvider provider) : base(provider)
    {
        
    }

    private async Task AddData()
    {
        using var tx = Connection.BeginTransaction();
        for (var l = 0; l < 10; l++)
        {
            var loadout = new Loadout.New(tx)
            {
                Name = $"Loadout {l}"
            };
            
            for (var m = 0; m < 10; m++)
            {
                var mod = new Mod.New(tx)
                {
                    Name = $"Mod {m}",
                    Description = $"{m}",
                    Source = new Uri($"http://mod{m}.com"),
                    LoadoutId = loadout
                };
                
                for (var f = 0; f < 10; f++)
                {
                    var file = new File.New(tx)
                    {
                        Path = $"File {f}",
                        ModId = mod,
                        Size = Size.FromLong(f * m * l),
                        Hash = Hash.FromLong(f * m * l)
                    };
                    
                }
            }
        }
        await tx.Commit();
    }

    [Test]
    public async Task CanAddIndex()
    {
        await AddData();

        var cache = Connection.AttributeCache;
        var aid = cache.GetAttributeId(Mod.Description.Id);
        await Assert.That(cache.IsIndexed(aid)).IsFalse();
        
        var withIndex = new StringAttribute(Mod.Description.Id.Namespace, Mod.Description.Id.Name) { IsIndexed = true, IsOptional = true };
        var prevTxId = Connection.Db.BasisTxId;

        await Connection.UpdateSchema(withIndex);

        await Assert.That(Connection.Db.BasisTxId.Value).IsEqualTo(prevTxId.Value + 1);
        await Assert.That(cache.IsIndexed(cache.GetAttributeId(Mod.Description.Id))).IsTrue();
        
        var foundByDocs = Connection.Db.Datoms(Mod.Description, "0").ToArray();
        await Assert.That(foundByDocs.Length).IsEqualTo(10);
    }

    [Test]
    public async Task CanRemoveIndex()
    {
        await AddData();

        var cache = Connection.AttributeCache;
        var aid = cache.GetAttributeId(Mod.Source.Id);
        await Assert.That(cache.IsIndexed(aid)).IsTrue();
        
        var withIndex = new UriAttribute(Mod.Source.Id.Namespace, Mod.Source.Id.Name) { IsIndexed = false };
        var prevTxId = Connection.Db.BasisTxId;

        await Connection.UpdateSchema(withIndex);

        await Assert.That(Connection.Db.BasisTxId.Value).IsEqualTo(prevTxId.Value + 1);
        await Assert.That(cache.IsIndexed(cache.GetAttributeId(Mod.Source.Id))).IsFalse();
        
        await Assert.That(() => Connection.Db.Datoms(Mod.Source, new Uri("http://mod0.com")).ToArray()).Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments("SDV.2_5_2025.rocksdb.zip")]
    public async Task CanOpenOlderDBs(string fileName)
    {
        var path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory) / "Resources/Databases" / fileName;

        await using var extractedFolder = TemporaryFileManager.CreateFolder();

        ZipFile.ExtractToDirectory(path.ToString(), extractedFolder.Path.ToString());

        var settings = new DatomStoreSettings()
        {
            Path = extractedFolder.Path / "MnemonicDB.rocksdb",
        };

        using var connection = ConnectionFactory.Create(Provider, settings);
        var db = connection.Db;
        var cache = connection.AttributeCache;
        foreach (var attr in AttributeDefinition.All(db))
        {
            var expected = cache.GetAttributeId(attr.UniqueId);
            
            await Assert.That(attr.Indexed).IsEqualTo(cache.GetIndexedFlags(expected));
        }
        
        return;
    }
}
