using System;
using System.Linq;
using System.Threading;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.Abstractions;

namespace NexusMods.MnemonicDB.InternalTxFunctions;

internal class SimpleMigration : AInternalFn
{
    private readonly IAttribute[] _declaredAttributes;
    private ulong _tempId = PartitionId.Temp.MakeEntityId(1).Value;

    private static string[] InternalNamespaces = ["NexusMods.MnemonicDB.DatomStore", "NexusMods.MnemonicDB.Transactions"]; 
    public SimpleMigration(IAttribute[] attributes)
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
        var datoms = new Datoms(store.AttributeResolver);
        var madeChanges = false;
        foreach (var attribute in _declaredAttributes)
        {
            // Internal transactions are migrated elsewhere
            if (InternalNamespaces.Contains(attribute.Id.Namespace))
                continue;
            
            if (!cache.TryGetAttributeId(attribute.Id, out var aid))
            {
                madeChanges = true;
                AddAttribute(attribute, datoms);
                continue;
            }

            if (cache.IsIndexed(aid) != attribute.IsIndexed)
            {
                if (attribute.IsIndexed)
                    AddIndex(store, aid, batch, attribute.IndexedFlags);
                else
                    RemoveIndex(store, aid, batch, attribute.IndexedFlags);
                madeChanges = true;
            }
            
            if (cache.GetValueTag(aid) != attribute.LowLevelType)
            {
                throw new Exception("Cannot convert types, write a manual migration");
            }
        }

        if (!madeChanges) 
            return;
        
        store.LogDatoms(batch, datoms, advanceTx: true);
        store.AttributeCache.Reset(store.CurrentSnapshot.MakeDb(store.AsOfTxId, store.AttributeResolver));
    }


    private void AddAttribute(IAttribute definition, in Datoms builder)
    {
        var id = TempId(PartitionId.Attribute);
        builder.Add(id, AttributeDefinition.UniqueId, definition.Id);
        builder.Add(id, AttributeDefinition.ValueType, definition.LowLevelType);
        builder.Add(id, AttributeDefinition.Cardinality, definition.Cardinalty);
        builder.Add(id, AttributeDefinition.Indexed, definition.IndexedFlags);
        
        if (definition.DeclaredOptional)
            builder.Add(id, AttributeDefinition.Optional, Null.Instance);
        
        if (definition.NoHistory)
            builder.Add(id, AttributeDefinition.NoHistory, Null.Instance);
    }

    /// <summary>
    /// Remove add indexed datoms for a specific attribute
    /// </summary>
    internal static void AddIndex(DatomStore store, AttributeId attrId, IWriteBatch batch, IndexedFlags newFlags)
    {
        foreach (var datom in store.CurrentSnapshot[attrId])
        {
            batch.Add(datom.With(IndexType.AVETCurrent));
        }
        
        foreach (var datom in store.CurrentSnapshot[attrId])
        {
            batch.Add(datom.With(IndexType.AVETHistory));
        }
        
        var datoms = new Datoms(store.AttributeResolver)
        {
            { EntityId.From(attrId.Value), AttributeDefinition.Indexed, newFlags }
        };
        store.LogDatoms(batch, datoms);
    }

    /// <summary>
    /// Remove the indexed datoms for a specific attribute
    /// </summary>
    internal static void RemoveIndex(DatomStore store, AttributeId id, IWriteBatch batch, IndexedFlags newFlags)
    {
        foreach (var datom in store.CurrentSnapshot[id])
        {
            batch.Delete(datom.With(IndexType.AVETCurrent));
        }
        
        foreach (var datom in store.CurrentSnapshot[id])
        {
            batch.Delete(datom.With(IndexType.AVETHistory));;
        }
        
        var datoms = new Datoms(store.AttributeResolver)
        {
            { EntityId.From(id.Value), AttributeDefinition.Indexed, newFlags }
        };

        store.LogDatoms(batch, datoms);
    }
}
