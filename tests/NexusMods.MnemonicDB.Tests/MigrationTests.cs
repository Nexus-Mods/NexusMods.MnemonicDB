using System.Net;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
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
}
