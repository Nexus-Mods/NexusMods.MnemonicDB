using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.MnemonicDB.TestModel.Attributes;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Tests;

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

    [Fact]
    public async Task CanAddIndex()
    {
        await AddData();

        var cache = Connection.AttributeCache;
        var aid = cache.GetAttributeId(Mod.Description.Id);
        cache.IsIndexed(aid).Should().BeFalse();
        
        var withIndex = new StringAttribute(Mod.Description.Id.Namespace, Mod.Description.Id.Name) { IsIndexed = true, IsOptional = true };
        var prevTxId = Connection.Db.BasisTxId;

        await Connection.UpdateSchema(withIndex);

        Connection.Db.BasisTxId.Value.Should().Be(prevTxId.Value + 1);
        cache.IsIndexed(cache.GetAttributeId(Mod.Description.Id)).Should().BeTrue();
        
        var foundByDocs = Connection.Db.Datoms(Mod.Description, "0").ToArray();
        foundByDocs.Length.Should().Be(10);
    }

    [Fact]
    public async Task CanRemoveIndex()
    {
        await AddData();

        var cache = Connection.AttributeCache;
        var aid = cache.GetAttributeId(Mod.Source.Id);
        cache.IsIndexed(aid).Should().BeTrue();
        
        var withIndex = new UriAttribute(Mod.Source.Id.Namespace, Mod.Source.Id.Name) { IsIndexed = false };
        var prevTxId = Connection.Db.BasisTxId;

        await Connection.UpdateSchema(withIndex);

        Connection.Db.BasisTxId.Value.Should().Be(prevTxId.Value + 1);
        cache.IsIndexed(cache.GetAttributeId(Mod.Source.Id)).Should().BeFalse();
        
        Action act = () => Connection.Db.Datoms(Mod.Source, new Uri("http://mod0.com")).ToArray();
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("SDV.2_5_2025.rocksdb.zip")]
    public async Task CanOpenOlderDBs(string fileName)
    {
        var path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory) / "Resources/Databases" / fileName;

        await using var extractedFolder = TemporaryFileManager.CreateFolder();

        ZipFile.ExtractToDirectory(path.ToString(), extractedFolder.Path.ToString());

        var settings = new DatomStoreSettings()
        {
            Path = extractedFolder.Path / "MnemonicDB.rocksdb",
        };
        using var backend = new Backend();
        using var store = new DatomStore(Provider.GetRequiredService<ILogger<DatomStore>>(), settings, backend);
        using var connection = new Connection(Provider.GetRequiredService<ILogger<Connection>>(), store, Provider, [], null, false);

        var db = connection.Db;
        var attrs = db.Datoms(AttributeDefinition.UniqueId);
        var datoms = new List<Datom>();
        foreach (var attr in attrs)
            datoms.AddRange(db.Datoms(attr.E));

        var cache = connection.AttributeCache;
        foreach (var attr in AttributeDefinition.All(db))
        {
            var expected = cache.GetAttributeId(attr.UniqueId);
            
            attr.Indexed.Should().Be(cache.GetIndexedFlags(expected), "The indexed flags are backwards compatible");
        }
        
        return;
    }
}
