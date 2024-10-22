using System.Threading;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.Abstractions;

namespace NexusMods.MnemonicDB.InternalTxFunctions;

internal class SchemaMigration : AInternalFn
{
    private readonly IAttribute[] _declaredAttributes;
    private ulong _tempId = PartitionId.Temp.MakeEntityId(1).Value;

    public SchemaMigration(IAttribute[] attributes)
    {
        _declaredAttributes = attributes;
    }
    

    /// <inhertdoc />
    public EntityId TempId(PartitionId entityPartition)
    {
        var tempId = Interlocked.Increment(ref _tempId);
        // Add the partition to the id
        var actualId = ((ulong)entityPartition << 40) | tempId;
        return EntityId.From(actualId);
    }
    
    public override void Execute(DatomStore store)
    {
        var batch = store.Backend.CreateBatch();
        var cache = store.AttributeCache;
        using var builder = new IndexSegmentBuilder(cache);
        var madeChanges = false;
        foreach (var attribute in _declaredAttributes)
        {
            if (!cache.TryGetAttributeId(attribute.Id, out var aid))
            {
                madeChanges = true;
                AddAttribute(attribute, builder);
                continue;
            }

            if (cache.IsIndexed(aid) != attribute.IsIndexed)
            {
                if (attribute.IsIndexed)
                    AddIndex(store, aid, batch);
                else
                    RemoveIndex(store, aid, batch);
                madeChanges = true;
            }
            
            if (cache.GetValueTag(aid) != attribute.LowLevelType)
            {
                store.Logger.LogInformation("Converting values for attribute {0} from {1} to {2}", attribute.Id, cache.GetValueTag(aid), attribute.ValueType);
                ConvertValuesTo(store, attribute.IsIndexed, aid, batch, attribute.LowLevelType);
                madeChanges = true;
            }
        }

        if (!madeChanges) 
            return;
        
        var built = builder.Build();
        store.LogDatoms(batch, built, advanceTx: true);
        store.AttributeCache.Reset(new Db(store.CurrentSnapshot, store.AsOfTxId, store.AttributeCache));
    }


    private void AddAttribute(IAttribute definition, in IndexSegmentBuilder builder)
    {
        var id = TempId(PartitionId.Attribute);
        builder.Add(id, AttributeDefinition.UniqueId, definition.Id);
        builder.Add(id, AttributeDefinition.ValueType, definition.LowLevelType);
        builder.Add(id, AttributeDefinition.Cardinality, definition.Cardinalty);
        
        if (definition.IsIndexed) 
            builder.Add(id, AttributeDefinition.Indexed, Null.Instance);
        
        if (definition.DeclaredOptional)
            builder.Add(id, AttributeDefinition.Optional, Null.Instance);
        
        if (definition.NoHistory)
            builder.Add(id, AttributeDefinition.NoHistory, Null.Instance);
    }

    /// <summary>
    /// Remove add indexed datoms for a specific attribute
    /// </summary>
    internal static void AddIndex(DatomStore store, AttributeId id, IWriteBatch batch)
    {
        foreach (var datom in store.CurrentSnapshot.Datoms(SliceDescriptor.Create(id, IndexType.AEVTCurrent)))
        {
            batch.Add(IndexType.AVETCurrent, datom);
        }
        
        foreach (var datom in store.CurrentSnapshot.Datoms(SliceDescriptor.Create(id, IndexType.AVETCurrent)))
        {
            batch.Add(IndexType.AVETHistory, datom);
        }
        
        using var builder = new IndexSegmentBuilder(store.AttributeCache);
        builder.Add(EntityId.From(id.Value), AttributeDefinition.Indexed, Null.Instance);
        var built = builder.Build();
        
        store.LogDatoms(batch, built);
    }

    /// <summary>
    /// Remove the indexed datoms for a specific attribute
    /// </summary>
    internal static void RemoveIndex(DatomStore store, AttributeId id, IWriteBatch batch)
    {
        foreach (var datom in store.CurrentSnapshot.Datoms(SliceDescriptor.Create(id, IndexType.AEVTCurrent)))
        {
            batch.Delete(IndexType.AVETCurrent, datom);
        }
        
        foreach (var datom in store.CurrentSnapshot.Datoms(SliceDescriptor.Create(id, IndexType.AVETCurrent)))
        {
            batch.Delete(IndexType.AVETHistory, datom);
        }
        
        using var builder = new IndexSegmentBuilder(store.AttributeCache);
        builder.Add(EntityId.From(id.Value), AttributeDefinition.Indexed, Null.Instance, true);
        var built = builder.Build();
        
        store.LogDatoms(batch, built);
    }

    internal static void ConvertValuesTo(DatomStore store, bool isIndexed, AttributeId id, IWriteBatch batch, ValueTag newTagType)
    {
        using var writer = new PooledMemoryBufferWriter();
        foreach (var datom in store.CurrentSnapshot.Datoms(SliceDescriptor.Create(id, IndexType.AEVTCurrent)))
        {
            batch.Delete(IndexType.EAVTCurrent, datom);
            batch.Delete(IndexType.AEVTCurrent, datom);
            batch.Delete(IndexType.TxLog, datom);
            
            
            var currentTag = datom.Prefix.ValueTag;
            
            // if it's a reference, delete it from the backref index 
            if (currentTag == ValueTag.Reference)
                batch.Delete(IndexType.VAETCurrent, datom);
            
            // Delete it from the Value index if it's not a reference
            if (isIndexed && currentTag != ValueTag.Reference)
                batch.Delete(IndexType.AVETCurrent, datom);
            
            // Convert the value to the new type
            var newDatom = ConvertValue(datom, writer, newTagType);
            
            // Put the converted datom back into the indexes
            batch.Add(IndexType.EAVTCurrent, newDatom);
            batch.Add(IndexType.AEVTCurrent, newDatom);
            batch.Add(IndexType.TxLog, newDatom);
            
            if (newTagType == ValueTag.Reference)
                batch.Add(IndexType.VAETCurrent, newDatom);
            
            if (isIndexed && newTagType != ValueTag.Reference)
                batch.Add(IndexType.AVETCurrent, newDatom);
        }
        
        foreach (var datom in store.CurrentSnapshot.Datoms(SliceDescriptor.Create(id, IndexType.AEVTHistory)))
        {
            batch.Delete(IndexType.EAVTHistory, datom);
            batch.Delete(IndexType.AEVTHistory, datom);
            batch.Delete(IndexType.TxLog, datom);
            
            var currentTag = datom.Prefix.ValueTag;
            
            // if it's a reference, delete it from the backref index 
            if (currentTag == ValueTag.Reference)
                batch.Delete(IndexType.VAETHistory, datom);
            
            // Delete it from the Value index if it's not a reference
            if (isIndexed && currentTag != ValueTag.Reference)
                batch.Delete(IndexType.AEVTHistory, datom);
            
            // Convert the value to the new type
            var newDatom = ConvertValue(datom, writer, newTagType);
            
            // Put the converted datom back into the indexes
            batch.Add(IndexType.EAVTHistory, newDatom);
            batch.Add(IndexType.AEVTHistory, newDatom);
            batch.Add(IndexType.TxLog, newDatom);
                        
            if (newTagType == ValueTag.Reference)
                batch.Add(IndexType.VAETHistory, newDatom);
            
            if (isIndexed && newTagType != ValueTag.Reference)
                batch.Add(IndexType.AVETHistory, newDatom);
        }
    }

    private static Datom ConvertValue(Datom datom, PooledMemoryBufferWriter writer, ValueTag newTagType)
    {
        writer.Reset();
        datom.Prefix.ValueTag.ConvertValue(datom.ValueSpan, newTagType, writer);
        var prefix = datom.Prefix with { ValueTag = newTagType };
        var newDatom = new Datom(prefix, writer.WrittenMemory);
        return newDatom;
    }
}
