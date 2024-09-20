using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.MnemonicDB.Storage.Tests.TestAttributes;
using NexusMods.MnemonicDB.TestModel;
using NexusMods.MnemonicDB.TestModel.Attributes;
using NexusMods.Paths;
using File = NexusMods.MnemonicDB.TestModel.File;

namespace NexusMods.MnemonicDB.Storage.Tests;

public abstract class AStorageTest : IDisposable
{
    private readonly AbsolutePath _path;
    private readonly IServiceProvider _provider;
    protected readonly DatomStoreSettings DatomStoreSettings;
    protected AttributeCache AttributeCache => DatomStore.AttributeCache;

    protected readonly ILogger Logger;

    private ulong _tempId = 1;
    protected IDatomStore DatomStore;

    protected AStorageTest(IServiceProvider provider, Func<IStoreBackend>? backendFn = null)
    {
        _provider = provider;
        
        _path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("tests_datomstore" + Guid.NewGuid());

        DatomStoreSettings = new DatomStoreSettings
        {
            Path = _path
        };

        backendFn ??= () => new Backend();


        DatomStore = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), DatomStoreSettings, backendFn());
        
        Logger = provider.GetRequiredService<ILogger<AStorageTest>>();
        
        using var segmentBuilder = new IndexSegmentBuilder(AttributeCache);
        AddAttr(segmentBuilder, File.Path, AttributeId.From(20));
        AddAttr(segmentBuilder, File.Hash, AttributeId.From(21));
        AddAttr(segmentBuilder, File.Size, AttributeId.From(22));
        AddAttr(segmentBuilder, File.ModId, AttributeId.From(23));
        AddAttr(segmentBuilder, Mod.Name, AttributeId.From(24));
        AddAttr(segmentBuilder, Mod.LoadoutId, AttributeId.From(25));
        AddAttr(segmentBuilder, Loadout.Name, AttributeId.From(26));
        AddAttr(segmentBuilder, Collection.Name, AttributeId.From(27));
        AddAttr(segmentBuilder, Collection.LoadoutId, AttributeId.From(28));
        AddAttr(segmentBuilder, Collection.ModIds, AttributeId.From(29));
        AddAttr(segmentBuilder, Blobs.InKeyBlob, AttributeId.From(30));
        AddAttr(segmentBuilder, Blobs.InValueBlob, AttributeId.From(31));
        var (_, db) = DatomStore.Transact(segmentBuilder.Build());
        AttributeCache.Reset(db);
    }

    private void AddAttr(IndexSegmentBuilder segmentBuilder, IAttribute attribute, AttributeId attributeId)
    { 
        segmentBuilder.Add(EntityId.From(attributeId.Value), AttributeDefinition.UniqueId, attribute.Id);
        segmentBuilder.Add(EntityId.From(attributeId.Value), AttributeDefinition.ValueType, attribute.LowLevelType);
        segmentBuilder.Add(EntityId.From(attributeId.Value), AttributeDefinition.Cardinality, attribute.Cardinalty);
        if (attribute.IsIndexed)
            segmentBuilder.Add(EntityId.From(attributeId.Value), AttributeDefinition.Indexed, Null.Instance);
        if (attribute.NoHistory)
            segmentBuilder.Add(EntityId.From(attributeId.Value), AttributeDefinition.NoHistory, Null.Instance);
        if (attribute.DeclaredOptional)
            segmentBuilder.Add(EntityId.From(attributeId.Value), AttributeDefinition.Optional, Null.Instance);
    }

    public void Dispose()
    {
    }

    public EntityId NextTempId()
    {
        var partition = PartitionId.Entity;
        var id = Interlocked.Increment(ref _tempId);
        id |= (ulong)partition << 40;
        return PartitionId.Temp.MakeEntityId(id);
    }
}
