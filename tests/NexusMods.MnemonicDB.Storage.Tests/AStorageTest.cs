using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Traits;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.Storage.Tests.TestAttributes;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Storage.Tests;

public abstract class AStorageTest : IDisposable
{
    private readonly AbsolutePath _path;
    private readonly IServiceProvider _provider;
    protected readonly DatomStoreSettings DatomStoreSettings;
    public Backend Backend { get; }

    protected AttributeCache AttributeCache => DatomStore.AttributeCache;

    protected readonly ILogger Logger;

    private ulong _tempId = 1;
    protected IDatomStore DatomStore;

    protected AStorageTest(IServiceProvider provider, bool isInMemory)
    {
        _provider = provider;
        
        _path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("tests_datomstore" + Guid.NewGuid());

        DatomStoreSettings = new DatomStoreSettings
        {
            Path = isInMemory ? null : _path,
        };

        Backend = new Backend();
        DatomStore = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), DatomStoreSettings, new Backend());
        
        Logger = provider.GetRequiredService<ILogger<AStorageTest>>();
        
        var tx = new List<IDatomLikeRO>();
        AddAttr(tx, File.Path, AttributeId.From(20));
        AddAttr(tx, File.Hash, AttributeId.From(21));
        AddAttr(tx, File.Size, AttributeId.From(22));
        AddAttr(tx, File.ModId, AttributeId.From(23));
        AddAttr(tx, Mod.Name, AttributeId.From(24));
        AddAttr(tx, Mod.LoadoutId, AttributeId.From(25));
        AddAttr(tx, Loadout.Name, AttributeId.From(26));
        AddAttr(tx, Collection.Name, AttributeId.From(27));
        AddAttr(tx, Collection.LoadoutId, AttributeId.From(28));
        AddAttr(tx, Collection.ModIds, AttributeId.From(29));
        AddAttr(tx, Blobs.InKeyBlob, AttributeId.From(30));
        AddAttr(tx, Blobs.InValueBlob, AttributeId.From(31));
        var (_, db) = DatomStore.Transact(tx);
        AttributeCache.Reset(db);
    }


    private void AddAttr(List<IDatomLikeRO> tx, IAttribute attribute, AttributeId attributeId)
    { 
        var eid = EntityId.From(attributeId.Value);
        tx.Add(ValueDatom.Create(eid, AttributeDefinition.UniqueId, attribute.Id, DatomStore.AttributeCache));
        tx.Add(ValueDatom.Create(eid, AttributeDefinition.ValueType, attribute.LowLevelType, DatomStore.AttributeCache));
        tx.Add(ValueDatom.Create(eid, AttributeDefinition.Cardinality, attribute.Cardinalty, DatomStore.AttributeCache));
        tx.Add(ValueDatom.Create(eid, AttributeDefinition.Indexed, attribute.IndexedFlags, DatomStore.AttributeCache));

        if (attribute.NoHistory)
            tx.Add(ValueDatom.Create(eid, AttributeDefinition.NoHistory, Null.Instance, DatomStore.AttributeCache));
        if (attribute.DeclaredOptional)
            tx.Add(ValueDatom.Create(eid, AttributeDefinition.Optional, Null.Instance, DatomStore.AttributeCache));
    }

    public void Dispose()
    {
        DatomStore.Dispose();
        Backend.Dispose();
    }

    public EntityId NextTempId()
    {
        var partition = PartitionId.Entity;
        var id = Interlocked.Increment(ref _tempId);
        id |= (ulong)partition << 40;
        return PartitionId.Temp.MakeEntityId(id);
    }
}
